using System.Security.Cryptography;

namespace Client.MVVM.Model
{
    public static class Cryptography
    {
        private const int pbkdf2Iterations = 100000;

        public static bool Compare(string password, byte[] salt, byte[] digest)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, pbkdf2Iterations,
                HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(16);
            for (int i = 0; i < key.Length; ++i)
                if (key[i] != digest[i])
                    return false;
            return true;
        }

        public static byte[] ComputeDigest(string password, byte[] salt) =>
            new Rfc2898DeriveBytes(password, salt, pbkdf2Iterations,
                HashAlgorithmName.SHA256).GetBytes(16);

        public static void GenerateRandom(byte[] bytes)
        {
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
        }

        public static byte[] GenerateSalt()
        {
            var bytes = new byte[16];
            GenerateRandom(bytes);
            return bytes;
        }
    }
}
