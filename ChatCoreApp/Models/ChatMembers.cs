using System.ComponentModel.DataAnnotations.Schema;

namespace ChatCoreApp.Models
{
    public class ChatMembers
    {
        public int ChatId { set; get; }

        public string UserId { set; get; }

        public Chat Chat { set; get; }

        public ApplicationUser User { set; get; }
    }
}
