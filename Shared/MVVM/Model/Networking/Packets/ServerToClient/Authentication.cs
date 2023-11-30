using Shared.MVVM.Model.Cryptography;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient
{
    public class Authentication : Packet
    {
        #region Fields
        public const Codes CODE = Codes.Authentication;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            ulong localSeed)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);
            pb.Append(localSeed, TOKEN_SIZE);
            // Pakiet szyfrowany i autentykowany
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
