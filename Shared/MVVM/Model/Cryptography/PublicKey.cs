using System.IO;
using System;
using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public class PublicKey
    {
        // często używana jako e liczba pierwsza Fermata 2^(2^4) + 1 65537; w big-endian
        public static readonly byte[] PUBLIC_EXPONENT = { 0b0000_0001, 0b0000_0000, 0b0000_0001 };

        private byte[] _modulus;

        private PublicKey(byte[] modulus)
        {
            _modulus = modulus;
        }

        public static Status TryParse(string text)
        {
            if (text == null)
                return new Status(-1, null, "|String is null.|"); // -1

            if (text == "")
                return new Status(-2, null, "|String is empty.|"); // -2

            try
            { return new Status(0, new PublicKey(Convert.FromBase64String(text))); } // 0
            catch (FormatException)
            { return new Status(-3, null, "|Number| |is not valid Base64 string.|"); } // -3
        }

        public override string ToString()
        {
            return Convert.ToBase64String(_modulus);
        }

        public static PublicKey FromBytes(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var lengthBuffer = new byte[2];

                ms.Read(lengthBuffer, 0, 2);
                var modulusLength = BitConverter.ToUInt16(lengthBuffer, 0);
                var modulus = new byte[modulusLength];
                ms.Read(modulus, 0, modulusLength);

                return new PublicKey(modulus);
            }
        }

        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(BitConverter.GetBytes((ushort)_modulus.Length), 0, 2);
                ms.Write(_modulus, 0, _modulus.Length);

                return ms.ToArray();
            }
        }

        public void ImportTo(RSA rsa)
        {
            var par = new RSAParameters();
            par.Exponent = PUBLIC_EXPONENT;
            par.Modulus = _modulus;
            rsa.ImportParameters(par);
        }
    }
}
