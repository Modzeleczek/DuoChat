using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer.Message
{
    public class SendMessage : Packet
    {
        #region Classes
        public enum Errors : byte
        {
            ConversationNotExists = 0,
            YouNotBelongToConversation = 1
        }

        public class Attachment
        {
            public byte[] EncryptedContent { get; set; } = null!;
        }

        public class Recipient
        {
            public ulong ParticipantId { get; set; }
            public byte[] EncryptedContent { get; set; } = null!;
            public Attachment[] Attachments { get; set; } = null!;
        }

        public class AttachmentMetadata
        {
            public string Name { get; set; } = null!;
        }

        public class Message
        {
            public ulong ConversationId { get; set; }
            public AttachmentMetadata[] AttachmentMetadatas { get; set; } = null!;
            public Recipient[] Recipients { get; set; } = null!;
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.SendMessage;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            Message message)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            SerializeMessage(ref pb, message);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        private static void SerializeMessage(ref PacketBuilder pb, Message message)
        {
            pb.Append(message.ConversationId, ID_SIZE);

            pb.Append((ulong)message.AttachmentMetadatas.Length, 1);
            foreach (var attachmentMetadata in message.AttachmentMetadatas)
                SerializeAttachmentMetadata(ref pb, attachmentMetadata);

            pb.Append((ulong)message.Recipients.Length, 1);
            foreach (var recipient in message.Recipients)
                SerializeRecipient(ref pb, recipient);
        }

        private static void SerializeAttachmentMetadata(ref PacketBuilder pb,
            AttachmentMetadata attachmentMetadata)
        {
            byte[] nameBytes = Encoding.UTF8.GetBytes(attachmentMetadata.Name);
            // if (nameBytes.Length > 255) throw
            pb.Append((ulong)nameBytes.Length, 1);
            pb.Append(nameBytes);
        }

        private static void SerializeRecipient(ref PacketBuilder pb, Recipient recipient)
        {
            pb.Append(recipient.ParticipantId, ID_SIZE);
            // if (recipient.EncryptedContent.Length > 65535) throw
            pb.Append((ulong)recipient.EncryptedContent.Length, 2);
            pb.Append(recipient.EncryptedContent);

            foreach (var attachment in recipient.Attachments)
                SerializeAttachment(ref pb, attachment);
        }

        private static void SerializeAttachment(ref PacketBuilder pb, Attachment attachment)
        {
            pb.Append((ulong)attachment.EncryptedContent.Length, 2);
            pb.Append(attachment.EncryptedContent);
        }

        public static void Deserialize(PacketReader pr,
            out Message message)
        {
            message = DeserializeMessage(pr);
        }

        private static Message DeserializeMessage(PacketReader pr)
        {
            var message = new Message { ConversationId = pr.ReadUInt64() };

            message.AttachmentMetadatas = new AttachmentMetadata[pr.ReadUInt8()];
            for (int i = 0; i < message.AttachmentMetadatas.Length; ++i)
                message.AttachmentMetadatas[i] = DeserializeAttachmentMetadata(pr);

            message.Recipients = new Recipient[pr.ReadUInt8()];
            for (int i = 0; i < message.Recipients.Length; ++i)
                message.Recipients[i] = DeserializeRecipient(pr, message.AttachmentMetadatas.Length);

            return message;
        }

        private static AttachmentMetadata DeserializeAttachmentMetadata(PacketReader pr)
        {
            return new AttachmentMetadata { Name = pr.ReadUtf8String(pr.ReadUInt8()) };
        }

        private static Recipient DeserializeRecipient(PacketReader pr, int attachmentCount)
        {
            var recipient = new Recipient
            {
                ParticipantId = pr.ReadUInt64(),
                EncryptedContent = pr.ReadBytes(pr.ReadUInt16()),
                Attachments = new Attachment[attachmentCount]
            };

            for (int i = 0; i < recipient.Attachments.Length; ++i)
                recipient.Attachments[i] = DeserializeAttachment(pr);

            return recipient;
        }

        private static Attachment DeserializeAttachment(PacketReader pr)
        {
            return new Attachment { EncryptedContent = pr.ReadBytes(pr.ReadUInt16()) };
        }
    }
}
