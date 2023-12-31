using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient.Message
{
    public class AttachmentContent : Packet
    {
        #region Classes
        public class Attachment
        {
            public string Name { get; set; } = null!;
            public byte[] EncryptedContent { get; set; } = null!;
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.AttachmentContent;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            Attachment attachment)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            SerializeAttachment(ref pb, attachment);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        private static void SerializeAttachment(ref PacketBuilder pb, Attachment attachment)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(attachment.Name);
            // if (nameBytes.Length > 255) throw
            pb.Append((ulong)nameBytes.Length, 1);
            pb.Append(nameBytes);

            // if (attachment.EncryptedContent.Length > (1 << 16)) throw
            pb.Append((ulong)attachment.EncryptedContent.Length, 2);
            pb.Append(attachment.EncryptedContent);
        }

        public static void Deserialize(PacketReader pr,
            out Attachment attachment)
        {
            attachment = DeserializeAttachment(pr);
        }

        private static Attachment DeserializeAttachment(PacketReader pr)
        {
            return new Attachment
            { Name = pr.ReadUtf8String(pr.ReadUInt8()), EncryptedContent = pr.ReadBytes(pr.ReadUInt16()) };
        }
    }
}
