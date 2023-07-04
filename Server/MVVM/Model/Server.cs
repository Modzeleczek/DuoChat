using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Shared.MVVM.Core;
using System;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.ViewModel.Results;

namespace Server.MVVM.Model
{
    public class Server
    {
        #region Properties
        public bool IsRunning { get; private set; } = false;
        #endregion

        #region Fields
        private List<Client> _clients = new List<Client>();
        private TcpListener _listener = null;
        private Guid _guid = Guid.Empty;
        private PrivateKey _privateKey = null;
        private int _capacity = 0;
        private Task _runner = null;
        private volatile bool _stopRequested = false;
        #endregion

        #region Events
        public event Callback Stopped;
        #endregion

        public Server() { }

        public void Start(Guid guid, PrivateKey privateKey, IPv4Address ipAddress, Port port,
            int capacity)
        {
            try
            {
                var localEndPoint = new IPEndPoint(ipAddress.ToIPAddress(), port.Value);
                _listener = new TcpListener(localEndPoint);
                _listener.Start(capacity);
                _guid = guid;
                _privateKey = privateKey;
                _capacity = capacity;
                _stopRequested = false;
                _runner = Task.Factory.StartNew(Process, TaskCreationOptions.LongRunning);
                IsRunning = true;
                return;
            }
            catch (SocketException se)
            {
                _listener.Stop();
                IsRunning = false;
                throw new Error(se, "|No translation:|");
            }
        }

        private void Process()
        {
            Result result = null;
            try
            {
                while (true)
                {
                    if (_stopRequested) break;
                    // https://stackoverflow.com/a/365533
                    if (!_listener.Pending())
                    {
                        Thread.Sleep(500); // choose a number (in milliseconds) that makes sense
                        continue; // skip to next iteration of loop
                    }

                    var client = new Client(_listener.AcceptTcpClient());
                    lock (_clients)
                    {
                        if (_clients.Count < _capacity)
                        {
                            // TODO: trzymać klientów w mapie, żeby przyspieszyć usuwanie
                            client.Disconnected += (s) =>
                            {
                                lock (_clients)
                                    _clients.Remove(client);
                            };
                            client.Introduce(_guid, _privateKey.ToPublicKey());
                            _clients.Add(client);
                        }
                        else // nie ma wolnych slotów
                            client.NoSlots();
                    }
                }
                foreach (var c in _clients)
                    c.Disconnect();
                result = new Success();
            }
            /* nie łapiemy InvalidOperationException, bo _listener.AcceptTcpClient()
            może je wyrzucić tylko jeżeli nie wywołaliśmy wcześniej _listener.Start() */
            catch (SocketException se)
            {
                /* według dokumentacji funkcji TcpListener.AcceptTcpClient,
                se.ErrorCode jest kodem błędu, którego opis można zobaczyć
                w "Windows Sockets version 2 API error code documentation" */
                result = new Failure(se, "|No translation:|");
            }
            finally
            {
                _clients.Clear();
                _listener.Stop();
                IsRunning = false;
                /* jeżeli nie ma żadnych obserwatorów (nikt nie ustawił callbacków
                (handlerów)) i Stopped == null, to Invoke się nie wykona */
                Stopped?.Invoke(result);
            }
        }

        public void RequestStop()
        {
            // jeżeli serwer nie działa, to udajemy, że się od razu zamknął
            if (!IsRunning)
            {
                Stopped?.Invoke(new Success());
                return;
            }
            _stopRequested = true;
        }
    }
}
