using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.MVVM.Model.Networking.Transfer.Reception
{
    public interface IReceiveSocket
    {
        ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags,
            CancellationToken cancellationToken);
    }
}
