using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer.Message
{
    public class GetMessages : Packet
    {
        #region Classes
        public enum Errors : byte
        {
            ConversationNotExists = 0,
            YouNotBelongToConversation = 1
        }

        public class Filter
        {
            public ulong ConversationId { get; set; }
            public byte FindNewest { get; set; } = 0;
            public ulong MessageId { get; set; }
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.GetMessages;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            Filter filter)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            SerializeFilter(ref pb, filter);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        private static void SerializeFilter(ref PacketBuilder pb, Filter filter)
        {
            pb.Append(filter.ConversationId, ID_SIZE);
            pb.Append(filter.FindNewest, 1);
            pb.Append(filter.MessageId, ID_SIZE);
        }

        public static void Deserialize(PacketReader pr,
            out Filter filter)
        {
            filter = DeserializeFilter(pr);
        }

        private static Filter DeserializeFilter(PacketReader pr)
        {
            return new Filter
            {
                ConversationId = pr.ReadUInt64(),
                FindNewest = pr.ReadUInt8(),
                MessageId = pr.ReadUInt64()
            };
        }
    }
}
