using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer.Conversation
{
    public class AddConversation : Packet
    {
        #region Classes
        public enum Errors : byte { }
        #endregion

        #region Fields
        public const Codes CODE = Codes.AddConversation;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            string conversationName)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            byte[] nameBytes = Encoding.UTF8.GetBytes(conversationName);
            // if (nameBytes.Length > 255) throw
            pb.Append((ulong)nameBytes.Length, 1);
            pb.Append(nameBytes);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr,
            out string conversationName)
        {
            conversationName = pr.ReadUtf8String(pr.ReadUInt8());
        }
    }
}
