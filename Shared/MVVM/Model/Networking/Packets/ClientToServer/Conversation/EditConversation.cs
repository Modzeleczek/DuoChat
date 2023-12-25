using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer.Conversation
{
    public class EditConversation : Packet
    {
        #region Classes
        public enum Errors : byte
        {
            ConversationNotExists = 0,
            RequesterNotConversationOwner = 1
        }

        public class Conversation
        {
            public ulong Id { get; set; }
            public string Name { get; set; } = null!;
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.EditConversation;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            Conversation conversation)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            pb.Append(conversation.Id, ID_SIZE);
            byte[] nameBytes = Encoding.UTF8.GetBytes(conversation.Name);
            // if (nameBytes.Length > 255) throw
            pb.Append((ulong)nameBytes.Length, 1);
            pb.Append(nameBytes);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr,
            out Conversation conversation)
        {
            conversation = new Conversation
            {
                Id = pr.ReadUInt64(),
                Name = pr.ReadUtf8String(pr.ReadUInt8())
            };
        }
    }
}
