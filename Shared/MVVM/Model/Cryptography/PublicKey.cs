using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public class PublicKey
    {
        // często używana jako e liczba pierwsza Fermata 2^(2^4) + 1 65537; w big-endian
        public static readonly byte[] PUBLIC_EXPONENT = { 0b0000_0001, 0b0000_0000, 0b0000_0001 };

        private byte[] _modulus;

        public PublicKey(byte[] modulus)
        {
            _modulus = modulus;
        }

        public void ImportTo(RSA rsa)
        {
            var par = new RSAParameters();
            par.Exponent = PUBLIC_EXPONENT;
            par.Modulus = _modulus;
            rsa.ImportParameters(par);
        }

        public byte[] ToBytes() => _modulus;
    }
}
