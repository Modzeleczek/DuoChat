using Shared.MVVM.Model.Cryptography;
using System;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer
{
    public class ClientIntroduction : Packet
    {
        #region Fields
        public const Codes CODE = Codes.ClientIntroduction;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            string login,
            ulong verificationToken,
            ulong localSeed)
        {
            byte[] loginBytes = Encoding.UTF8.GetBytes(login);
            // Długość zserializowanego loginu musi mieścić się na 1 bajcie.
            if (loginBytes.Length > 255)
                // Nieprawdopodobne
                throw new ArgumentOutOfRangeException(nameof(login),
                    "UTF-8 encoded client's login can be at most 255 bytes long.");

            PublicKey clientPublicKey = senderPrivateKey.ToPublicKey();
            if (clientPublicKey.Length > 256)
                // Nieprawdopodobne
                throw new ArgumentOutOfRangeException(nameof(senderPrivateKey),
                    "Public key from client's private key can be at most 256 bytes long.");

            var pb = new PacketBuilder();
            pb.Append((ulong)loginBytes.Length, 1);
            pb.Append(loginBytes);
            pb.Append(verificationToken, TOKEN_SIZE);
            pb.Append(localSeed, TOKEN_SIZE);
            pb.Sign(senderPrivateKey);
            /* Długość klucza znajduje się już w bajtach
            zwróconych przez PublicKey.ToBytes(). */
            pb.Prepend(clientPublicKey.ToBytes());
            pb.Encrypt(receiverPublicKey);
            pb.Prepend((byte)CODE, 1);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr, PrivateKey receiverPrivateKey,
            out PublicKey publicKey,
            out bool senderSignatureValid,
            out string login,
            out ulong verificationToken,
            out ulong remoteSeed)
        {
            // Kod operacji musi być wcześniej odczytany.
            pr.Decrypt(receiverPrivateKey);

            publicKey = PublicKey.FromPacketReader(pr);
            senderSignatureValid = pr.VerifySignature(publicKey);
            byte loginLength = pr.ReadUInt8();
            /* Długość loginu jest zapisana na 1 bajcie,
            czyli może mieć wartość maksymalnie 255. */
            login = pr.ReadUtf8String(loginLength);
            verificationToken = pr.ReadUInt64();
            remoteSeed = pr.ReadUInt64();
        }
    }
}
