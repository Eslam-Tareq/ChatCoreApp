namespace ChatCoreApp.Hubs
{
    public interface IChatHub
    {
        public Task ReceiveMessage();
    }
}
