using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient.Message
{
    public class DisplayedMessage : Packet
    {
        #region Classes
        public class Display
        {
            public ulong ConversationId { get; set; }
            public ulong MessageId { get; set; }
            // Uczestnik lub właściciel konwersacji.
            public ulong RecipientId { get; set; }
            public long ReceiveTime { get; set; }
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.DisplayedMessage;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            Display display)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            SerializeDisplay(ref pb, display);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        private static void SerializeDisplay(ref PacketBuilder pb, Display display)
        {
            pb.Append(display.ConversationId, ID_SIZE);
            pb.Append(display.MessageId, ID_SIZE);
            pb.Append(display.RecipientId, ID_SIZE);
            pb.Append((ulong)display.ReceiveTime, 8);
        }

        public static void Deserialize(PacketReader pr,
            out Display display)
        {
            display = DeserializeDisplay(pr);
        }

        private static Display DeserializeDisplay(PacketReader pr)
        {
            return new Display
            {
                ConversationId = pr.ReadUInt64(),
                MessageId = pr.ReadUInt64(),
                RecipientId = pr.ReadUInt64(),
                ReceiveTime = (long)pr.ReadUInt64()
            };
        }
    }
}
