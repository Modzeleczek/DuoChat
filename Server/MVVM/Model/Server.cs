using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;

namespace Server.MVVM.Model
{
    public class Server
    {
        public bool Started { get; private set; } = false;

        private List<Client> _clients = new List<Client>();
        private TcpListener _listener = null;
        private string _name = "";
        private uint _capacity = 0;
        private bool _acceptConnections = false;

        public Server() { }

        public void Start(int ipV4Address, ushort port, string name, uint capacity)
        {
            if (_listener != null) _listener.Stop();
            _clients.Clear();
            _name = name;
            _capacity = capacity;
            _listener = new TcpListener(new IPAddress(ipV4Address), port);
            _listener.Start();
            Started = true;
        }

        public void Process()
        {
            _acceptConnections = true;
            while (_acceptConnections)
            {
                var cli = new Client(_listener.AcceptTcpClient());
                _clients.Add(cli);

            }
        }

        public void Stop()
        {
            if (_listener == null || !Started) return;
            _listener.Stop();
        }
    }
}
