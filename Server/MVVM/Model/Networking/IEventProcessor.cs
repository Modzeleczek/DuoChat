namespace Server.MVVM.Model.Networking
{
    public interface IEventProcessor
    {
        void Enqueue(ClientEvent @event);
    }
}
