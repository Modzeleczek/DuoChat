using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer.Conversation
{
    public class DeleteConversation : Packet
    {
        #region Classes
        public enum Errors : byte
        {
            ConversationNotExists = 0,
            AccountNotConversationOwner = 1
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.DeleteConversation;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            ulong conversationId)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            pb.Append(conversationId, ID_SIZE);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr,
            out ulong conversationId)
        {
            conversationId = pr.ReadUInt64();
        }
    }
}
