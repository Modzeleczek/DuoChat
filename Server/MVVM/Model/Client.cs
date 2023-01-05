using System;
using System.Net.Sockets;

namespace Server.MVVM.Model
{
    public class Client
    {
        private TcpClient _socket;
        private bool _shouldDisconnect;

        public Client(TcpClient socket)
        {
            _socket = socket;
            _shouldDisconnect = false;
        }

        public void Process()
        {
            var netStr = _socket.GetStream();
            try
            {
                while (!_shouldDisconnect)
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
