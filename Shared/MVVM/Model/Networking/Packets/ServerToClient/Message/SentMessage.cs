using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient.Message
{
    public class SentMessage : Packet
    {
        #region Classes
        public class MessageMetadata
        {
            public ulong MessageId { get; set; }
            public ulong ConversationId { get; set; }
            public ulong SenderId { get; set; }
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.SentMessage;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            MessageMetadata messageMetadata)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            SerializeMessageMetadata(ref pb, messageMetadata);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        private static void SerializeMessageMetadata(ref PacketBuilder pb, MessageMetadata message)
        {
            pb.Append(message.MessageId, ID_SIZE);
            pb.Append(message.ConversationId, ID_SIZE);
            pb.Append(message.SenderId, ID_SIZE);
        }

        public static void Deserialize(PacketReader pr,
            out MessageMetadata messageMetadata)
        {
            messageMetadata = DeserializeMessageMetadata(pr);
        }

        private static MessageMetadata DeserializeMessageMetadata(PacketReader pr)
        {
            return new MessageMetadata
            { MessageId = pr.ReadUInt64(), ConversationId = pr.ReadUInt64(), SenderId = pr.ReadUInt64() };
        }
    }
}

