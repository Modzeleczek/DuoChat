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
            AccountAlreadyBlocked = 5, IPNowBlocked = 6, ConversationsAndUsersLists = 7,
            RequestError = 8, AddedConversation = 9, EditedConversation = 10,
            DeletedConversation = 11, FoundUsersList = 12, AddedParticipation = 13,
            AddedYouAsParticipant = 14, EditedParticipation = 15, DeletedParticipation = 16,
            SentMessage = 17, MessagesList = 18,

            ClientIntroduction = 255, GetConversationsAndUsers = 254, AddConversation = 253,
            EditConversation = 252, DeleteConversation = 251, SearchUsers = 250,
            AddParticipation = 249, EditParticipation = 248, DeleteParticipation = 247,
            SendMessage = 246, GetMessages = 245
        }
        #endregion

        #region Fields
        public const int TOKEN_SIZE = sizeof(ulong);
        protected const int ID_SIZE = sizeof(ulong);
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
