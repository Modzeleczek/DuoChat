using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    public static class ByteArrayExtensions
    {
        public static void BytesEqual(this byte[] expected, byte[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; ++i)
                Assert.AreEqual(expected[i], actual[i]);
        }
    }
}
