using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public static class RandomGenerator
    {
        public static byte[] Generate(int byteCount)
        {
            var bytes = new byte[byteCount];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return bytes;
        }
    }
}
