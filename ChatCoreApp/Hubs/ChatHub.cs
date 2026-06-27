using ChatCoreApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatCoreApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatCoreContext _context;

        public ChatHub(ChatCoreContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _context.UserConnections.Add(new UserConnection()
                {
                    UserId = userId,
                    ConnectionId = Context.ConnectionId
                });
                await _context.SaveChangesAsync();

                // Join SignalR groups for user's existing chats
                var userChatIds = await _context.ChatMembers
                    .Where(cm => cm.UserId == userId)
                    .Select(cm => cm.ChatId)
                    .ToListAsync();

                foreach (var chatId in userChatIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
                }
            }

            await SendInitialData(userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connection = await _context.UserConnections
                .FirstOrDefaultAsync(uc => uc.ConnectionId == Context.ConnectionId);

            if (connection != null)
            {
                _context.UserConnections.Remove(connection);
                await _context.SaveChangesAsync();
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task SendInitialData(string? userId)
        {
            // All available group rooms
            var allAvailableGroups = await _context.Chats
                .Where(chat => chat.Type == ChatType.Group && chat.Group != null)
                .Select(c => new { chatId = c.Id, name = c.Group!.Name })
                .ToListAsync();

            await Clients.All.SendAsync("AllAvaliableGroups", allAvailableGroups);

            if (!string.IsNullOrEmpty(userId))
            {
                // Current user's joined groups
                var currentGroups = await _context.ChatMembers
                    .Where(cm => cm.UserId == userId && cm.Chat.Type == ChatType.Group && cm.Chat.Group != null)
                    .Select(cm => new { chatId = cm.ChatId, name = cm.Chat.Group!.Name })
                    .ToListAsync();

                await Clients.Caller.SendAsync("CurrentGroups", currentGroups);

                // All other registered users for private messaging
                var allUsers = await _context.Users
                    .Where(u => u.Id != userId)
                    .Select(u => new { id = u.Id, userName = u.UserName })
                    .ToListAsync();

                await Clients.Caller.SendAsync("AllUsers", allUsers);
            }
        }

        public async Task CreateGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return;

            string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var group = new Group { Name = groupName };
            _context.Groups.Add(group);

            var chat = new Chat
            {
                Type = ChatType.Group,
                Group = group
            };
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                _context.ChatMembers.Add(new ChatMembers
                {
                    ChatId = chat.Id,
                    UserId = userId
                });
                await _context.SaveChangesAsync();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, chat.Id.ToString());

            // Broadcast updated group list to all
            var allAvailableGroups = await _context.Chats
                .Where(c => c.Type == ChatType.Group && c.Group != null)
                .Select(c => new { chatId = c.Id, name = c.Group!.Name })
                .ToListAsync();

            await Clients.All.SendAsync("AllAvaliableGroups", allAvailableGroups);

            // Refresh creator's groups
            if (!string.IsNullOrEmpty(userId))
            {
                await RefreshUserGroups(userId);
            }
        }

        public async Task DeleteGroup(int chatId)
        {
            var chat = await _context.Chats
                .Include(c => c.Group)
                .Include(c => c.Messages)
                .Include(c => c.ChatMembers)
                .FirstOrDefaultAsync(c => c.Id == chatId && c.Type == ChatType.Group);

            if (chat != null)
            {
                var memberUserIds = chat.ChatMembers?.Select(m => m.UserId).ToList() ?? new List<string>();

                if (chat.Messages != null && chat.Messages.Any())
                    _context.Messages.RemoveRange(chat.Messages);

                if (chat.ChatMembers != null && chat.ChatMembers.Any())
                    _context.ChatMembers.RemoveRange(chat.ChatMembers);

                if (chat.Group != null)
                    _context.Groups.Remove(chat.Group);

                _context.Chats.Remove(chat);
                await _context.SaveChangesAsync();

                // Broadcast updated available groups to everyone
                var allAvailableGroups = await _context.Chats
                    .Where(c => c.Type == ChatType.Group && c.Group != null)
                    .Select(c => new { chatId = c.Id, name = c.Group!.Name })
                    .ToListAsync();

                await Clients.All.SendAsync("AllAvaliableGroups", allAvailableGroups);

                // Refresh groups for members of deleted group
                foreach (var memberId in memberUserIds)
                {
                    await RefreshUserGroups(memberId);
                }
            }
        }

        public async Task JoinGroup(int chatId)
        {
            string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            var exists = await _context.ChatMembers
                .AnyAsync(cm => cm.ChatId == chatId && cm.UserId == userId);

            if (!exists)
            {
                _context.ChatMembers.Add(new ChatMembers { ChatId = chatId, UserId = userId });
                await _context.SaveChangesAsync();
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());
            await RefreshUserGroups(userId);
        }

        public async Task LeaveGroup(int chatId)
        {
            string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return;

            var member = await _context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == userId);

            if (member != null)
            {
                _context.ChatMembers.Remove(member);
                await _context.SaveChangesAsync();
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());
            await RefreshUserGroups(userId);
        }

        public async Task SendGroupMessage(int chatId, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string senderName = Context.User?.Identity?.Name ?? "Anonymous";
            if (string.IsNullOrEmpty(userId)) return;

            var chat = await _context.Chats
                .Include(c => c.Group)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null) return;

            var msg = new Message
            {
                ChatId = chatId,
                SenderId = userId,
                Content = message
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            string groupName = chat.Group?.Name ?? "Group";
            await Clients.Group(chatId.ToString()).SendAsync("ReceiveGroupMessage", senderName, groupName, message);
        }

        public async Task SendPrivateMessage(string receiverId, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrEmpty(receiverId)) return;

            string? senderId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string senderName = Context.User?.Identity?.Name ?? "Anonymous";
            if (string.IsNullOrEmpty(senderId)) return;

            // Find existing private chat between sender and receiver
            var privateChat = await _context.Chats
                .Include(c => c.ChatMembers)
                .Where(c => c.Type == ChatType.Private)
                .FirstOrDefaultAsync(c => c.ChatMembers!.Any(m => m.UserId == senderId)
                                       && c.ChatMembers!.Any(m => m.UserId == receiverId));

            if (privateChat == null)
            {
                privateChat = new Chat { Type = ChatType.Private };
                _context.Chats.Add(privateChat);
                await _context.SaveChangesAsync();

                _context.ChatMembers.Add(new ChatMembers { ChatId = privateChat.Id, UserId = senderId });
                _context.ChatMembers.Add(new ChatMembers { ChatId = privateChat.Id, UserId = receiverId });
                await _context.SaveChangesAsync();
            }

            var msg = new Message
            {
                ChatId = privateChat.Id,
                SenderId = senderId,
                Content = message
            };
            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            var targetConnections = await _context.UserConnections
                .Where(uc => uc.UserId == senderId || uc.UserId == receiverId)
                .Select(uc => uc.ConnectionId)
                .ToListAsync();

            if (targetConnections.Any())
            {
                await Clients.Clients(targetConnections).SendAsync("ReceivePrivateMessage", senderName, receiverId, message);
            }
            else
            {
                await Clients.User(receiverId).SendAsync("ReceivePrivateMessage", senderName, receiverId, message);
                await Clients.Caller.SendAsync("ReceivePrivateMessage", senderName, receiverId, message);
            }
        }

        private async Task RefreshUserGroups(string userId)
        {
            var currentGroups = await _context.ChatMembers
                .Where(cm => cm.UserId == userId && cm.Chat.Type == ChatType.Group && cm.Chat.Group != null)
                .Select(cm => new { chatId = cm.ChatId, name = cm.Chat.Group!.Name })
                .ToListAsync();

            var connectionIds = await _context.UserConnections
                .Where(uc => uc.UserId == userId)
                .Select(uc => uc.ConnectionId)
                .ToListAsync();

            if (connectionIds.Any())
            {
                await Clients.Clients(connectionIds).SendAsync("CurrentGroups", currentGroups);
            }
            else
            {
                await Clients.User(userId).SendAsync("CurrentGroups", currentGroups);
            }
        }
    }
}
