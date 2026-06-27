using Microsoft.AspNetCore.Identity;

namespace ChatCoreApp.Models
{
    public class ApplicationUser:IdentityUser
    {
        public ICollection<Message> Messages { set; get; }

        public ICollection<ChatMembers> ChatMembers { set; get; }
        public ICollection<UserConnection> userConnections { set; get; }
    }
}
