using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer
{
    public class AddConversation : Packet
    {
        #region Classes
        public enum Errors : byte
        {
            AccountDoesNotExist = 0
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.AddConversation;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            ulong ownerId,
            string name)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);
            pb.Append(ownerId, 8);
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            pb.Append((ulong)nameBytes.Length, 1);
            pb.Append(nameBytes);
            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr,
            out ulong ownerId,
            out string name)
        {
            ownerId = pr.ReadUInt64();
            name = pr.ReadUtf8String(pr.ReadUInt8());
        }
    }
}
