using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient
{
    public class NoAuthentication : Packet
    {
        #region Fields
        public const Codes CODE = Codes.NoAuthentication;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed)
        {
            return SerializeSignedEncryptedWithOnlyOperationCode(CODE, tokenFromRemoteSeed,
                senderPrivateKey, receiverPublicKey);
        }

        public void Deserialize(PacketReader pr,
            out ulong token)
        {
            token = pr.ReadUInt64();
        }
    }
}
