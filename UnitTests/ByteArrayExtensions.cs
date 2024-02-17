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

        public static void BytesEqual(this IEnumerable<byte> expected, IEnumerable<byte> actual)
        {
            var expIterator = expected.GetEnumerator();
            var actIterator = actual.GetEnumerator();
            while (true)
            {
                bool expHasNext = expIterator.MoveNext();
                bool actHasNext = actIterator.MoveNext();
                /* Jeżeli jeden ma następną wartość, a drugi nie ma,
                to znaczy, że nie mają równych długości. */
                if (expHasNext != actHasNext)
                    Assert.Fail("expected and actual have different lengths");
                // Oba już nie mają następnej wartości.
                if (!expHasNext)
                    return;
                // Oba jeszcze mają następną wartość.
                Assert.AreEqual(expIterator.Current, actIterator.Current);
            }   
        }
    }
}
