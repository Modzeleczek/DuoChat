using System.IO;
using System;
using System.Security.Cryptography;
using Shared.MVVM.Core;

namespace Shared.MVVM.Model.Cryptography
{
    public class PublicKey : RsaKey
    {
        private const int LENGTH_BYTE_COUNT = sizeof(ushort);
        // często używana jako e liczba pierwsza Fermata 2^(2^4) + 1 65537; w big-endian
        public static readonly byte[] PUBLIC_EXPONENT = { 0b0000_0001, 0b0000_0000, 0b0000_0001 };

        private byte[] _modulus;

        public PublicKey(byte[] modulus)
        {
            _modulus = modulus;
        }

        public static PublicKey Parse(string text)
        {
            if (text == null)
                throw new Error("|String is null.|");

            if (text == "")
                throw new Error("|String is empty.|");

            try { return new PublicKey(Convert.FromBase64String(text)); }
            catch (FormatException e)
            { throw new Error(e, "|Number| |is not valid Base64 string.|"); }
        }

        public override string ToString()
        {
            return Convert.ToBase64String(_modulus);
        }

        public static PublicKey FromBytes(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var lengthBuffer = new byte[LENGTH_BYTE_COUNT];

                ms.Read(lengthBuffer, 0, LENGTH_BYTE_COUNT);
                ushort modulusLength = BitConverter.ToUInt16(lengthBuffer, 0);
                var modulus = new byte[modulusLength];
                ms.Read(modulus, 0, modulusLength);

                return new PublicKey(modulus);
            }
        }

        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(BitConverter.GetBytes((ushort)_modulus.Length), 0, LENGTH_BYTE_COUNT);
                ms.Write(_modulus, 0, _modulus.Length);

                return ms.ToArray();
            }
        }

        public override void ImportTo(RSA rsa)
        {
            var par = new RSAParameters();
            par.Exponent = PUBLIC_EXPONENT;
            par.Modulus = _modulus;
            rsa.ImportParameters(par);
        }
    }
}
