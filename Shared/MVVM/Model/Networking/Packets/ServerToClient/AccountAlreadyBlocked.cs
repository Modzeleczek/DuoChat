using Shared.MVVM.Model.Cryptography;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient
{
    public class AccountAlreadyBlocked : Packet
    {
        #region Fields
        public const Codes CODE = Codes.AccountAlreadyBlocked;
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
