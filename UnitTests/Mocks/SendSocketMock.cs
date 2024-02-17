using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Net.Sockets;

namespace UnitTests.Mocks
{
    public class SendSocketMock : ISendSocket
    {
        #region Fields
        private readonly int[] _returnedByteCounts;
        private int _returnedByteCountIndex = 0;
        public byte[] ByteStream { get; }
        private int _byteStreamIndex = 0;
        #endregion

        public SendSocketMock(int[] returnedByteCounts)
        {
            _returnedByteCounts = returnedByteCounts;
            ByteStream = new byte[returnedByteCounts.Sum()];
        }

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException("SendSocketMock SendAsync canceled");

            if (_returnedByteCountIndex >= _returnedByteCounts.Length)
                return new ValueTask<int>(0);

            int returnedByteCount = _returnedByteCounts[_returnedByteCountIndex];
            ++_returnedByteCountIndex;
            int bufferLimit = buffer.Length;
            int byteStreamLimit = ByteStream.Length - _byteStreamIndex;

            if (bufferLimit < returnedByteCount)
                throw new ArgumentException("buffer cannot be smaller " +
                    "than returnedByteCount", nameof(buffer));

            /* Jeżeli bufor jest większy od returnedByteCount, to obcinamy go i nie przepisujemy
            nadmiarowych bajtów do strumienia. W ten sposób sumulujemy sytuację, w której socket
            wysłał nie wszystkie bajty, które mu przekazaliśmy. */
            bufferLimit = returnedByteCount;

            if (bufferLimit > byteStreamLimit)
                throw new ArgumentException("buffer shrinked to returnedByteCount cannot be bigger " +
                    "than byte stream remaining bytes count", nameof(buffer));

            using (var handle = buffer.Pin())
            {
                unsafe
                {
                    byte* p = (byte*)handle.Pointer;
                    for (int i = 0; i < bufferLimit; ++i)
                    {
                        ByteStream[_byteStreamIndex] = *(p + i);
                        ++_byteStreamIndex;
                    }
                }
            }

            return new ValueTask<int>(returnedByteCount);
        }

        public byte[] GetSentBytes()
        {
            return ByteStream.Slice(0, _byteStreamIndex);
        }
    }
}
