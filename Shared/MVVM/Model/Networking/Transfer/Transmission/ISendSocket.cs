using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.MVVM.Model.Networking.Transfer.Transmission
{
    public interface ISendSocket
    {
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags,
            CancellationToken cancellationToken);
    }
}
