using System;
using System.Net.Sockets;

namespace Server.MVVM.Model
{
    public class Client
    {
        private TcpClient _socket;
        public bool ShouldDisconnect { get; set; }

        public Client(TcpClient socket)
        {
            _socket = socket;
            ShouldDisconnect = false;
        }

        public void Process()
        {
            var netStr = _socket.GetStream();
            try
            {
                while (!ShouldDisconnect)
                {

                }
            }
            catch (Exception) { }
            finally
            {
                netStr.Dispose();
                _socket.Close();
            }
        }
    }
}
