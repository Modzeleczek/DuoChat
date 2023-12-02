using Shared.MVVM.Model.Networking;
using System.Net.Sockets;

namespace UnitTests.Mocks
{
    class ReceiveSocketMock : IReceiveSocket
    {
        #region Fields
        private readonly int[] _returnedByteCounts;
        private int _returnedByteCountIndex = 0;
        private readonly byte[] _byteStream;
        private int _byteStreamIndex = 0;
        #endregion

        public ReceiveSocketMock(int[] returnedByteCounts, byte[] byteStream)
        {
            _returnedByteCounts = returnedByteCounts;
            _byteStream = byteStream;

            if (_returnedByteCounts.Sum() != _byteStream.Length)
                throw new ArgumentException("returnedByteCounts sum must be " +
                    "equal to byteStream.Length");
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
        {
            if (_returnedByteCountIndex >= _returnedByteCounts.Length)
                return new ValueTask<int>(0);

            int returnedByteCount = _returnedByteCounts[_returnedByteCountIndex];
            ++_returnedByteCountIndex;
            int bufferLimit = buffer.Length;
            int byteStreamLimit = _byteStream.Length - _byteStreamIndex;
            // Znajdujemy minimum.
            if (returnedByteCount > bufferLimit)
                returnedByteCount = bufferLimit;
            if (returnedByteCount > byteStreamLimit)
                returnedByteCount = byteStreamLimit;

            using (var handle = buffer.Pin())
            {
                unsafe
                {
                    byte* p = (byte*)handle.Pointer;
                    for (int i = 0; i < returnedByteCount; ++i)
                    {
                        *(p + i) = _byteStream[_byteStreamIndex];
                        ++_byteStreamIndex;
                    }
                }
            }

            return new ValueTask<int>(returnedByteCount);
        }
    }
}
