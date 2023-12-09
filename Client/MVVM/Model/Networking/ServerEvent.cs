namespace Client.MVVM.Model.Networking
{
    public class ServerEvent
    {
        #region Classes
        public enum Types : byte
        {
            SendSuccess = 0, SendError, SendTimeout,
            ReceiveSuccess, ServerClosedSocket, ReceiveError, ReceiveTimeout
        }
        #endregion

        #region Properties
        public Types Type { get; }
        public RemoteServer Sender { get; }
        public object? Data { get; }
        #endregion

        public ServerEvent(Types type, RemoteServer sender, object? data = null)
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
