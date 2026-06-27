using System.ComponentModel.DataAnnotations.Schema;

namespace ChatCoreApp.Models
{
    public class Message
    {
        public int Id { set; get; }

        public string Content { set; get; }

        public int ChatId { set; get; }

        public Chat Chat { set; get; }


        public string SenderId { set; get; }


        public ApplicationUser Sender { set; get; }
    }
}
