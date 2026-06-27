using System.ComponentModel.DataAnnotations.Schema;

namespace ChatCoreApp.Models
{
    public class UserConnection
    {
        public string UserId { set; get; }

        public string ConnectionId { set; get; }
        [ForeignKey("UserId")]
        public ApplicationUser User { set; get; }

    }
}
