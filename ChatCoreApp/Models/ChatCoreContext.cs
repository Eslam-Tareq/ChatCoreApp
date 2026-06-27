using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatCoreApp.Models
{
    public class ChatCoreContext: IdentityDbContext<ApplicationUser>
    {
        public ChatCoreContext(DbContextOptions<ChatCoreContext> options):base(options)
        {
            
        }
        protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    builder.Entity<ChatMembers>()
        .HasKey(x => new { x.ChatId, x.UserId });

    builder.Entity<ChatMembers>()
        .HasOne(x => x.User)
        .WithMany(x => x.ChatMembers)
        .HasForeignKey(x => x.UserId);

    builder.Entity<ChatMembers>()
        .HasOne(x => x.Chat)
        .WithMany(x => x.ChatMembers)
        .HasForeignKey(x => x.ChatId);

    builder.Entity<Message>()
        .HasOne(x => x.Sender)
        .WithMany(x => x.Messages)
        .HasForeignKey(x => x.SenderId);

    builder.Entity<Message>()
        .HasOne(x => x.Chat)
        .WithMany(x => x.Messages)
        .HasForeignKey(x => x.ChatId);


            builder.Entity<UserConnection>().HasKey(cu => new { cu.UserId, cu.ConnectionId });
}

        public DbSet<Message> Messages { set; get; }

        public DbSet<Group> Groups { get; set; }



        public DbSet<ChatMembers> ChatMembers { set; get; }


        public DbSet<Chat> Chats { set; get; }


        public DbSet<UserConnection> UserConnections { set; get; }

    }
}
