using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer.Message
{
    public class GetAttachment : Packet
    {
        #region Classes
        public enum Errors : byte
        {
            AttachmentNotExists = 0,
            YouNotBelongToConversation = 1,
            YouNotRecipientOfMessage = 2
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.GetAttachment;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            ulong attachmentId)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            pb.Append(attachmentId, ID_SIZE);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr,
            out ulong attachmentId)
        {
            attachmentId = pr.ReadUInt64();
        }
    }
}
