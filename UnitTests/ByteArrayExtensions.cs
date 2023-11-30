using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace UnitTests
{
    public static class ByteArrayExtensions
    {
        public static string ToHexString(this byte[] array, int startIndex, int count)
        {
            var sb = new StringBuilder();
            for (int i = startIndex; i < startIndex + count; ++i)
                sb.Append($"{array[i]:X2} ");
            return sb.ToString();
        }
        
        public static string ToHexString(this byte[] array)
        {
            return ToHexString(array, 0, array.Length);
        }

        public static void BytesEqual(this byte[] expected, byte[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; ++i)
                Assert.AreEqual(expected[i], actual[i]);
        }

        public static byte[] Slice(this byte[] array, int startIndex, int count)
        {
            byte[] ret = new byte[count];
            Buffer.BlockCopy(array, startIndex, ret, 0, count);
            return ret;
        }

        public static byte[] Slice(this byte[] array, int startIndex)
        {
            return array.Slice(startIndex, array.Length - startIndex);
        }
    }
}
