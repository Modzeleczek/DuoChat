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
        #region Classes
        public enum ClientState
        {
            Connected = 0, ServerIntroduced, ClientAuthenticated
        }
        #endregion

        #region Properties
        // Właściwości do dyspozycji klasy Server i viewmodeli.
        public ClientState State { get; set; }
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
        public event Action<byte[]> ReceivedRequest;
        #endregion

        public Client(TcpClient socket)
        {
            _socket = socket;
            /* Zapisujemy referencję, bo po rozłączeniu klienta (_socket.Close)
            w GetPrimaryKey _socket.Client jest nullem. */
            _remoteEndPoint = (IPEndPoint)_socket.Client.RemoteEndPoint;
            ResetFlags();
        }

        public void StartProcessing()
        {
            State = ClientState.Connected;
            _runner = Task.Factory.StartNew(Process, TaskCreationOptions.LongRunning);
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

        protected override void OnReceivedPacket(byte[] packet)
        {
            // Wątek Client.Handle
            ReceivedRequest(packet);
        }

        public void Authenticate(string login, PublicKey publicKey)
        {
            if (State >= ClientState.ClientAuthenticated)
                // Nieprawdopodobne
                throw new Error("Client already authenticated.");

            Login = login;
            PublicKey = publicKey;
        }
    }
}
