using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.View.Localization;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Client.MVVM.Model
{
    public class Client
    {
        #region Properties
        public bool IsConnected { get; private set; } = false;
        #endregion

        #region Fields
        private TcpClient _socket = null;
        private Task _runner = null;
        private bool _disconnectRequested = false;
        private Server _server = null;
        #endregion

        #region Events
        public event Callback Disconnected;
        #endregion

        public Client() { }

        public Status Connect(Server server)
        {
            Status status = null;
            try
            {
                var remoteEndPoint = new IPEndPoint(server.IpAddress.ToIPAddress(), server.Port);
                _socket = new TcpClient();
                _socket.Connect(remoteEndPoint);
                _server = server;
                _disconnectRequested = false;
                _runner = Task.Run(Process);
                IsConnected = true;
                status = new Status(0);
            }
            catch (Exception ex) when (
                ex is SocketException ||
                ex is ArgumentOutOfRangeException)
            {
                _socket.Close();
                IsConnected = false;
                var d = Translator.Instance;
                if (ex is SocketException)
                {
                    var se = (SocketException)ex;
                    status = new Status(-1, d["No translation: "] + se.Message, se.ErrorCode);
                }
                else // if (ex is ArgumentOutOfRangeException)
                {
                    var ae = (ArgumentOutOfRangeException)ex;
                    status = new Status(-2, d["No translation: "] + ae.Message);
                }
            }
            return status;
        }

        private void Process()
        {
            Status status = null;
            try
            {
                var stream = _socket.GetStream();
                while (true)
                {
                    lock (_socket) if (_disconnectRequested) break;
                }
                status = new Status(0);
            }
            catch (Exception ex)
            {
                var d = Translator.Instance;
                status = new Status(-1, d["No translation: "] + ex.Message);
            }
            finally
            {
                _socket.Close();
                IsConnected = false;
                Disconnected?.Invoke(status);
            }
        }

        public Status Disconnect()
        {
            Status ret = null;
            Callback disconnectHandler = (status) => ret = status;
            Disconnected += disconnectHandler;
            RequestDisconnect();
            // czekamy na zakończenie wątku (taska) obsługującego klienta
            _runner.Wait();
            Disconnected -= disconnectHandler;
            return ret;
        }

        public void RequestDisconnect()
        {
            if (!IsConnected) return;
            lock (_socket) _disconnectRequested = true;
        }
    }
}
