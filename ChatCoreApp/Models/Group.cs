namespace ChatCoreApp.Models
{
    public class Group
    {
        public int Id { set; get; }

        public string Name { set; get; }

        public int? ChatId { set; get; }

        public Chat Chat { set; get; }
    }
}
