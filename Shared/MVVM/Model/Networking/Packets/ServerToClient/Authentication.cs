using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;

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

        public static void Deserialize(PacketReader pr,
            out ulong remoteSeed)
        {
            remoteSeed = pr.ReadUInt64();
        }
    }
}
