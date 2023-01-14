using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Shared.MVVM.Model;
using Shared.MVVM.Core;
using System;
using Shared.MVVM.Model.Cryptography;

namespace Server.MVVM.Model
{
    public class Server
    {
        public bool Running { get; private set; } = false;

        private List<Client> _clients = new List<Client>();
        private TcpListener _listener = null;
        private Guid _guid = Guid.Empty;
        private PrivateKey _privateKey = null;
        private string _name = "";
        private int _capacity = 0;
        private Task _runner = null;
        private bool _stopRequested = false;

        public event Callback Started, Stopped;

        public Server() { }

        public void BeginStart(Guid guid, PrivateKey privateKey, int ipV4Address, ushort port,
            string name, int capacity)
        {
            Status status = null;
            try
            {
                _listener = new TcpListener(new IPAddress(ipV4Address), port);
                _listener.Start(capacity);
                _guid = guid;
                _privateKey = privateKey;
                _name = name;
                _capacity = capacity;
                _stopRequested = false;
                _runner = Task.Run(Process);
                Running = true;
                status = new Status(0);
            }
            catch (SocketException se)
            {
                _listener.Stop();
                // se.ErrorCode zawsze jest różne od 0
                status = new Status(-1, se.Message, se.ErrorCode);
            }
            finally
            {
                Started?.Invoke(status);
            }
        }

        private void Process()
        {
            Status status = null;
            try
            {
                while (true)
                {
                    lock (_listener) if (_stopRequested) break;
                    // https://stackoverflow.com/a/365533
                    if (!_listener.Pending())
                    {
                        Thread.Sleep(500); // choose a number (in milliseconds) that makes sense
                        continue; // skip to next iteration of loop
                    }
                    if (!(_clients.Count < _capacity)) continue;
                    var client = new Client(_listener.AcceptTcpClient());
                    _clients.Add(client);
                    client.Start();
                }
                foreach (var c in _clients)
                    c.Disconnect();
                status = new Status(0);
            }
            // nie łapiemy InvalidOperationException, bo _listener.AcceptTcpClient() może je wyrzucić tylko jeżeli nie wywołaliśmy wcześniej _listener.Start()
            catch (SocketException se)
            {
                // według dokumentacji funkcji TcpListener.AcceptTcpClient, se.ErrorCode jest kodem błędu, którego opis można zobaczyć w "Windows Sockets version 2 API error code documentation"
                status = new Status(-1, se.Message, se.ErrorCode);
            }
            finally
            {
                _clients.Clear();
                _listener.Stop();
                Running = false;
                Stopped?.Invoke(status); // jeżeli nie ma żadnych obserwatorów (nikt nie ustawił callbacków (handlerów)) i Stopped == null, to Invoke się nie wykona
            }
        }

        public void BeginStop()
        {
            // jeżeli serwer nie działa, to udajemy, że się oda razu zamknął
            if (_listener == null)
            {
                Stopped?.Invoke(new Status(0));
                return;
            }
            lock (_listener) _stopRequested = true;
        }
    }
}
