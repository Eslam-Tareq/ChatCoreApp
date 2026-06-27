namespace ChatCoreApp.Models
{
    public enum ChatType
    {
        Private=1,
        Group=2
    }
    public class Chat
    {
        public int Id { set; get; }

        public ChatType Type { set; get; }

        public Group? Group { get; set; }  

        public ICollection<Message>? Messages { set; get; }
        public ICollection<ChatMembers>? ChatMembers { set; get; }



    }
}
