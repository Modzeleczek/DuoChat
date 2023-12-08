using System.IO;
using System;
using System.Security.Cryptography;
using Shared.MVVM.Core;
using System.Net;
using System.Collections.Generic;
using System.Data.SQLite;
using Shared.MVVM.Model.Networking.Transfer.Reception;

namespace Shared.MVVM.Model.Cryptography
{
    public class PublicKey
    {
        #region Properties
        public int Length => _modulus.Length;
        #endregion

        #region Fields
        public const int LENGTH_BYTE_COUNT = sizeof(ushort);
        // często używana jako e liczba pierwsza Fermata 2^(2^4) + 1 65537; w big-endian
        public static readonly byte[] PUBLIC_EXPONENT =
            { 0b0000_0001, 0b0000_0000, 0b0000_0001 };

        private readonly byte[] _modulus;
        #endregion

        public PublicKey(byte[] modulus)
        {
            _modulus = modulus;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is PublicKey other))
                return false;

            if (other._modulus is null)
                return _modulus is null;

            if (_modulus is null)
                return false;

            if (other._modulus.Length != _modulus.Length)
                return false;

            for (int i = 0; i < _modulus.Length; i++)
                if (other._modulus[i] != _modulus[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = 805403592;
            hashCode = hashCode * -1521134295 + Length.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(_modulus);
            return hashCode;
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

        public void ImportTo(RSA rsa)
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

        public static PublicKey FromPacketReader(PacketReader reader)
        {
            /* Klucza publicznego nie da się zwalidować inaczej niż próbując
            nim odszyfrować wiadomość zaszyfrowaną RSA-OAEP. */
            var length = reader.ReadUInt16();

            if (length > 256)
                throw new Error("|Public key can be at most 256 bytes long|.");

            var bytes = reader.ReadBytes(length);
            return new PublicKey(bytes);
        }

        public static PublicKey FromSQLiteDataReader(SQLiteDataReader reader, int columnIndex)
        {
            var lengthBuffer = new byte[LENGTH_BYTE_COUNT];
            if (reader.GetBytes(columnIndex, 0, lengthBuffer, 0, LENGTH_BYTE_COUNT)
                != LENGTH_BYTE_COUNT)
                throw new Error("|Error occured while| |reading| " +
                    "|public key length| |from SQLiteDataReader|.");

            short signedLength = BitConverter.ToInt16(lengthBuffer, 0);
            ushort length = (ushort)IPAddress.NetworkToHostOrder(signedLength);
            var modulusBuffer = new byte[length];
            if (reader.GetBytes(columnIndex, 0, modulusBuffer, 0, length) != length)
                throw new Error("|Error occured while| |reading| " +
                    "|public key value| |from SQLiteDataReader|.");

            return new PublicKey(modulusBuffer);
        }
    }
}
