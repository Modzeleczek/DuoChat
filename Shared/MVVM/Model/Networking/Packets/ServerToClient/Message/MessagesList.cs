using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient.Message
{
    public class MessagesList : Packet
    {
        #region Classes
        public class AttachmentMetadata
        {
            public ulong Id { get; set; }
            public string Name { get; set; } = null!;
        }

        public class Message
        {
            public ulong Id { get; set; }
            public byte SenderExists { get; set; }
            public ulong SenderId { get; set; }
            public long SendTime { get; set; }
            public byte[] EncryptedContent { get; set; } = null!;
            public AttachmentMetadata[] AttachmentMetadatas { get; set; } = null!;
        }

        public class List
        {
            public ulong ConversationId { get; set; }
            public Message[] Messages { get; set; } = null!;
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.MessagesList;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            List list)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            SerializeList(ref pb, list);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        private static void SerializeList(ref PacketBuilder pb, List list)
        {
            pb.Append(list.ConversationId, ID_SIZE);

            pb.Append((ulong)list.Messages.Length, 1);
            foreach (var message in list.Messages)
                SerializeMessage(ref pb, message);
        }

        private static void SerializeMessage(ref PacketBuilder pb, Message message)
        {
            pb.Append(message.Id, ID_SIZE);
            pb.Append(message.SenderExists, 1);
            pb.Append(message.SenderId, ID_SIZE);
            pb.Append((ulong)message.SendTime, 8);
            // if (message.EncryptedContent.Length > 65535) throw
            pb.Append((ulong)message.EncryptedContent.Length, 2);
            pb.Append(message.EncryptedContent);

            pb.Append((ulong)message.AttachmentMetadatas.Length, 1);
            foreach (var attachmentMetadata in message.AttachmentMetadatas)
                SerializeAttachmentMetadata(ref pb, attachmentMetadata);
        }

        private static void SerializeAttachmentMetadata(ref PacketBuilder pb,
            AttachmentMetadata attachmentMetadata)
        {
            pb.Append(attachmentMetadata.Id, ID_SIZE);
            byte[] nameBytes = Encoding.UTF8.GetBytes(attachmentMetadata.Name);
            // if (nameBytes.Length > 255) throw
            pb.Append((ulong)nameBytes.Length, 1);
            pb.Append(nameBytes);
        }

        public static void Deserialize(PacketReader pr,
            out List list)
        {
            list = DeserializeList(pr);
        }

        private static List DeserializeList(PacketReader pr)
        {
            var list = new List
            {
                ConversationId = pr.ReadUInt64(),
                Messages = new Message[pr.ReadUInt8()]
            };

            for (int i = 0; i < list.Messages.Length; ++i)
                list.Messages[i] = DeserializeMessage(pr);

            return list;
        }

        private static Message DeserializeMessage(PacketReader pr)
        {
            var message = new Message
            {
                Id = pr.ReadUInt64(),
                SenderExists = pr.ReadUInt8(),
                SenderId = pr.ReadUInt64(),
                SendTime = (long)pr.ReadUInt64(),
                EncryptedContent = pr.ReadBytes(pr.ReadUInt16()),
                AttachmentMetadatas = new AttachmentMetadata[pr.ReadUInt8()]
            };

            for (int i = 0; i < message.AttachmentMetadatas.Length; ++i)
                message.AttachmentMetadatas[i] = DeserializeAttachmentMetadata(pr);

            return message;
        }

        private static AttachmentMetadata DeserializeAttachmentMetadata(PacketReader pr)
        {
            return new AttachmentMetadata { Id = pr.ReadUInt64(), Name = pr.ReadUtf8String(pr.ReadUInt8()) };
        }
    }
}
