using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
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
        public IPEndPoint RemoteEndPoint { get => (IPEndPoint)_socket.Client.RemoteEndPoint; }
        #endregion

        public Client(TcpClient socket)
        {
            _socket = socket;
            ResetFlags();
            _runner = Task.Factory.StartNew(Process, TaskCreationOptions.LongRunning);
        }

        public void Introduce(Guid guid, PublicKey publicKey)
        {
            // 0 Przedstawienie się serwera (00)
            var keyBytes = publicKey.ToBytes();
            // var bb = new PacketBuilder() + guid.ToByteArray() + (keyBytes.Length, 2) + keyBytes;
            var pb = new PacketBuilder();
            pb.Append(guid.ToByteArray());
            pb.Append(keyBytes.Length, 2);
            pb.Append(keyBytes);
            EnqueueToSend(0, pb);
        }

        public void NoSlots()
        {
            // 1 Brak wolnych połączeń (slotów) (00)
            EnqueueToSend(1, new PacketBuilder(), () => StopProcessing());
        }
    }
}
