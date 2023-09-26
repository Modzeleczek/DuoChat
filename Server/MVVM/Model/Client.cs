using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.ViewModel.Results;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BaseClient = Shared.MVVM.Model.Networking.Client;

namespace Server.MVVM.Model
{
    public class Client : BaseClient
    {
        #region Properties
        // Właściwości do dyspozycji klasy Server i viewmodeli.
        public byte[] TokenCache { get; set; } = null;

        public string Login { get; private set; } = null;
        public PublicKey PublicKey { get; private set; } = null;
        #endregion

        #region Fields
        private readonly IPEndPoint _remoteEndPoint;
        #endregion

        #region Events
        // Przekazujemy obserwatorom poprzedni i następny stan klienta.
        public event Action<Result> EndedConnection;
        #endregion

        public Client(TcpClient socket)
        {
            _socket = socket;
            /* Zapisujemy referencję, bo po rozłączeniu klienta (_socket.Close)
            w GetPrimaryKey _socket.Client jest nullem. */
            _remoteEndPoint = (IPEndPoint)_socket.Client.RemoteEndPoint;
            ResetFlags();
        }

        public void StartProcessing(Func<Result> processProtocol)
        {
            _runner = Task.Factory.StartNew(() => Process(processProtocol),
                TaskCreationOptions.LongRunning);
        }

        public ClientPrimaryKey GetPrimaryKey()
        {
            // Klucz główny podłączonego klienta jest tylko do odczytu.
            var re = _remoteEndPoint;
            return new ClientPrimaryKey(
                new IPv4Address(re.Address), new Port((ushort)re.Port));
        }

        protected override void OnEndedConnection(Result result)
        {
            // Wątek Client.Process
            EndedConnection(result);
        }

        public void Authenticate(string login, PublicKey publicKey)
        {
            if (!(Login is null && PublicKey is null))
                // Nieprawdopodobne
                throw new Error("Client already authenticated.");

            Login = login;
            PublicKey = publicKey;
        }
    }
}
