using Shared.MVVM.Model.Cryptography;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer
{
    public class GetConversationsAndUsers : Packet
    {
        #region Fields
        public const Codes CODE = Codes.GetConversationsAndUsers;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);
            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        public void Deserialize(PacketReader pr,
            out ulong token)
        {
            token = pr.ReadUInt64();
        }
    }
}
