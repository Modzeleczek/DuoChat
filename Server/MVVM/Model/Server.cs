using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;

namespace Server.MVVM.Model
{
    public class Server
    {
        private List<Client> _clients = new List<Client>();
        private TcpListener _listener;
        private string _name;
        private int _capacity;
        private bool _shouldStop;

        public Server() { }

        public void Start(uint ipAddress, ushort port, string name, int capacity)
        {
            if (_listener != null)
                _listener.Stop();
            _clients.Clear();
            _name = name;
            _capacity = capacity;
            _shouldStop = false;
            _listener = new TcpListener(new IPAddress(ipAddress), port);
            _listener.Start();
        }

        public void Process()
        {
            while (!_shouldStop)
            {
                var cli = new Client(_listener.AcceptTcpClient());
                _clients.Add(cli);

            }
        }

        public void Stop()
        {
            if (_listener == null) return;
            _listener.Stop();
        }
    }
}
