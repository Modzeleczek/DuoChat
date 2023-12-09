namespace Server.MVVM.Model.Networking
{
    public class ClientEvent
    {
        #region Classes
        public enum Types : byte
        {
            SendSuccess = 0, SendError, SendTimeout,
            ReceiveSuccess, ClientClosedSocket, ReceiveError, ReceiveTimeout
        }
        #endregion

        #region Properties
        public Types Type { get; }
        public Client Sender { get; }
        public object? Data { get; }
        #endregion

        public ClientEvent(Types type, Client sender, object? data = null)
        {
            Type = type;
            Sender = sender;
            Data = data;
        }

        public string ToDebugString()
        {
            return $"{Type} {Sender} {Data}";
        }
    }
}
