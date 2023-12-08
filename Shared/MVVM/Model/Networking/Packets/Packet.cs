using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Transmission;

namespace Shared.MVVM.Model.Networking.Packets
{
    public abstract class Packet
    {
        #region Classes
        public enum Codes : byte
        {
            NoSlots = 0, IPAlreadyBlocked = 1, ServerIntroduction = 2,
            Authentication = 3, NoAuthentication = 4,
            AccountAlreadyBlocked = 5, IPNowBlocked = 6, ConversationsAndUsersList = 7,
            RequestError = 8,

            ClientIntroduction = 255, GetConversationsAndUsers = 254, AddConversation = 253
        }
        #endregion

        #region Fields
        public const int TOKEN_SIZE = sizeof(ulong);
        #endregion

        protected static byte[] SerializeSignedEncryptedWithOnlyOperationCode(Codes operation,
            ulong tokenFromRemoteSeed, PrivateKey localPrivateKey, PublicKey remotePublicKey)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)operation, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);
            // Pakiet szyfrowany i autentykowany
            pb.Sign(localPrivateKey);
            pb.Encrypt(remotePublicKey);
            return pb.Build();
        }
    }
}
