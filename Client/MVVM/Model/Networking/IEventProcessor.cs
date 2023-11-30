namespace Client.MVVM.Model.Networking
{
    public interface IEventProcessor
    {
        void Enqueue(ServerEvent @event);
    }
}
