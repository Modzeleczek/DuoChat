using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Server.MVVM.Model
{
    public class Client
    {
        public bool Disposed { get; private set; } = false;

        private TcpClient _socket = null;
        private Task _runner = null;
        private bool _disconnectRequested = false;

        public Client(TcpClient socket)
        {
            _socket = socket;
        }

        public void Start()
        {
            _disconnectRequested = false;
            _runner = Task.Run(Process);
            Disposed = false;
        }

        private void Process()
        {
            try
            {
                var stream = _socket.GetStream();
                while (true)
                {
                    lock (_socket) if (_disconnectRequested) break;
                }
            }
            catch (Exception) { }
            finally
            {
                // zamiast _socket.Close() może być _socket.Dispose(), bo Close jedyne co robi to wywołuje Dispose
                _socket.Close();
                Disposed = true;
            }
        }

        public void Disconnect()
        {
            if (Disposed) return;
            lock (_socket) _disconnectRequested = true;
            // czekamy na zakończenie wątku (taska) obsługującego klienta
            _runner.Wait();
        }
    }
}
