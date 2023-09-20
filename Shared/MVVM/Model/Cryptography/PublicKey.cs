using System.IO;
using System;
using System.Security.Cryptography;
using Shared.MVVM.Core;
using System.Net;

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
            return FromBytes(bytes, 0, bytes.Length);
        }

        public static PublicKey FromBytes(byte[] bytes, int startIndex, int count)
        {
            using (var ms = new MemoryStream(bytes, startIndex, count))
            {
                var lengthBuffer = new byte[LENGTH_BYTE_COUNT];
                if (ms.Read(lengthBuffer, 0, LENGTH_BYTE_COUNT) != LENGTH_BYTE_COUNT)
                    throw new Error("|Error occured while| |reading| " +
                        "|public key length| |from byte sequence|.");

                short signedLength = BitConverter.ToInt16(lengthBuffer, 0);
                ushort length = (ushort)IPAddress.NetworkToHostOrder(signedLength);
                var modulusBuffer = new byte[length];
                if (ms.Read(modulusBuffer, 0, length) != length)
                    throw new Error("|Error occured while| |reading| " +
                        "|public key value| |from byte sequence|.");

                return new PublicKey(modulusBuffer);
            }
        }

        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            {
                short signedLength = IPAddress.HostToNetworkOrder((short)_modulus.Length);
                ms.Write(BitConverter.GetBytes(signedLength), 0, LENGTH_BYTE_COUNT);
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

        public static PublicKey FromBytesNoLength(byte[] bytes)
        {
            return new PublicKey(bytes);
        }

        public byte[] ToBytesNoLength()
        {
            /* BSON i SQLite zapisują długość tablicy bajtów, więc nie
            musimy sami go zapisywać jak w metodzie ToBytes. */
            return _modulus;
        }
    }
}
