using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.MVVM.Model.Networking.Transfer
{
    class SocketWrapper : IReceiveSocket, ISendSocket
    {
        #region Fields
        public const int PACKET_PREFIX_SIZE = sizeof(int);

        private readonly Socket _socket;
        #endregion

        public SocketWrapper(Socket socket)
        {
            _socket = socket;
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags,
            CancellationToken cancellationToken)
        {
            return _socket.ReceiveAsync(buffer, socketFlags, cancellationToken);
        }

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags,
            CancellationToken cancellationToken)
        {
            return _socket.SendAsync(buffer, socketFlags, cancellationToken);
        }
    }
}
