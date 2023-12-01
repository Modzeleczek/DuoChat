using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.MVVM.Model.Networking.Reception
{
    class SocketWrapper : IReceiveSocket
    {
        #region Fields
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
