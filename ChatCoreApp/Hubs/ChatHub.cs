using ChatCoreApp.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatCoreApp.Hubs
{
    public class ChatHub:Hub
    {
        private ChatCoreContext _context { set; get; }

        public ChatHub(ChatCoreContext context)
        {
            _context = context;   
        }
        public override Task OnConnectedAsync()
        {
            string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _context.UserConnections
                .Add(new UserConnection()
                {
                    UserId=userId,
                    ConnectionId=Context.ConnectionId
                });
            _context.SaveChanges();


            //get all groups 

            var AllAvaliableGroups = _context.Chats.Where(chat=>chat.Type==ChatType.Group).Include(chat => chat.Group)
                .Select(c => new
                {
                    c.Group.Name,
                    c.Group.Id,

                })
                ;
            var CurrentGroups = _context.ChatMembers
                    .Where(cm => cm.UserId == userId)
                    .Include(cm => cm.Chat)
                        .ThenInclude(c => c.Group)
                    .Select(cm => new { cm.Chat.Group.Name,cm.Chat.Group.Id,cm.ChatId })
                    .ToList();
            
            foreach (var item in CurrentGroups)
            {
                Groups.AddToGroupAsync(Context.ConnectionId, item.Name);   
            }


            var AllUsers = _context.Users.Where(u=>u.Id==userId).ToList();

            Clients.User(userId).SendAsync("CurrentGroups", CurrentGroups);
            Clients.All.SendAsync("AllAvaliableGroups", AllAvaliableGroups);
            Clients.All.SendAsync("AllUsers", AllUsers);

            return base.OnConnectedAsync();
        }

        public void CreateGroup(string GroupName)
        {
            var group = new Group()
            {
                Name = GroupName
            };

            _context.Add(group);

            _context.SaveChanges();

            Groups.AddToGroupAsync(Context.ConnectionId, GroupName);

            var chat = new Chat()
            {
                Type=ChatType.Group,
                Group=group
            };

            _context.Add(chat);

            _context.SaveChanges();


        }
    }
}
