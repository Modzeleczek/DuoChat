using Shared.MVVM.Model.Networking.Transfer.Reception;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.MVVM.Model.Networking.Transfer
{
    class SocketWrapper : IReceiveSocket
    {
        #region Fields
        public const int PACKET_PREFIX_SIZE = sizeof(int);

        private readonly Socket _socket;
        #endregion

        public SocketWrapper(Socket socket)
        {
            _socket = socket;
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
        {
            return _socket.ReceiveAsync(buffer, socketFlags, cancellationToken);
        }
    }
}
