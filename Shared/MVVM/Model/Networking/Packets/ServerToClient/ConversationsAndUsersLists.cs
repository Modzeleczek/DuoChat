using Shared.MVVM.Model.Cryptography;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient
{
    public class ConversationsAndUsersLists : Packet
    {
        #region Classes
        public class ConversationModel
        {
            public ulong Id { get; set; }
            public ulong OwnerId { get; set; }
            public string Name { get; set; } = null!;
        }

        public class ParticipantModel
        {
            public ulong ParticipantId { get; set; }
            public long JoinTime { get; set; }
            public byte IsAdministrator { get; set; }
        }

        public class ConversationParticipationModel
        {
            public ConversationModel Conversation { get; set; } = null!;
            public ParticipantModel[] Participants { get; set; } = null!;
        }

        public class AccountModel
        {
            public ulong Id { get; set; }
            public string Login { get; set; } = null!;
            public PublicKey PublicKey { get; set; } = null!;
            public byte IsBlocked { get; set; }
        }

        public class ConversationsAndUsersListModel
        {
            public ConversationParticipationModel[] ConversationParticipants { get; set; } = null!;
            public AccountModel[] Accounts { get; set; } = null!;
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.ConversationsAndUsersList;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            ConversationsAndUsersListModel model)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            pb.Append((ulong)model.ConversationParticipants.Length, 1);
            foreach (var cp in model.ConversationParticipants)
            {
                SerializeConversationTo(ref pb, cp.Conversation);

                pb.Append((ulong)cp.Participants.Length, 1);
                foreach (var p in cp.Participants)
                    SerializeParticipationTo(ref pb, p);
            }

            pb.Append((ulong)model.Accounts.Length, 1);
            foreach (var a in model.Accounts)
                SerializeAccountTo(ref pb, a);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        private static void SerializeConversationTo(ref PacketBuilder pb, ConversationModel conversation)
        {
            pb.Append(conversation.Id, 8);
            pb.Append(conversation.OwnerId, 8);
            byte[] nameBytes = Encoding.UTF8.GetBytes(conversation.Name);
            // if (nameBytes.Length > 255) throw
            pb.Append((ulong)nameBytes.Length, 1);
            pb.Append(nameBytes);
        }

        private static void SerializeParticipationTo(ref PacketBuilder pb,  ParticipantModel participant)
        {
            pb.Append(participant.ParticipantId, 8);
            pb.Append((ulong)participant.JoinTime, 8);
            pb.Append(participant.IsAdministrator, 1);
        }

        private static void SerializeAccountTo(ref PacketBuilder pb, AccountModel account)
        {
            pb.Append(account.Id, 8);
            byte[] loginBytes = Encoding.UTF8.GetBytes(account.Login);
            // if (loginBytes.Length > 255) throw
            pb.Append((ulong)loginBytes.Length, 1);
            pb.Append(account.PublicKey.ToBytes());
            pb.Append(account.IsBlocked, 1);
        }

        public static void Deserialize(PacketReader pr,
            out ConversationsAndUsersListModel model)
        {
            byte conversationsParticipationsCount = pr.ReadUInt8();
            var conversationParticipations =
                new ConversationParticipationModel[conversationsParticipationsCount];
            for (int cp = 0; cp < conversationsParticipationsCount; ++cp)
            {
                var conversation = DeserializeConversation(pr);

                byte participantsCount = pr.ReadUInt8();
                var participants = new ParticipantModel[participantsCount];
                for (int p = 0; p < participantsCount; ++p)
                    participants[p] = DeserializeParticipant(pr);

                conversationParticipations[cp] = new ConversationParticipationModel
                {
                    Conversation = conversation,
                    Participants = participants
                };
            }

            byte accountsCount = pr.ReadUInt8();
            var accounts = new AccountModel[accountsCount];
            for (int a = 0; a < accountsCount; ++a)
                accounts[a] = DeserializeAccount(pr);

            model = new ConversationsAndUsersListModel
            {
                ConversationParticipants = conversationParticipations,
                Accounts = accounts
            };
        }

        private static ConversationModel DeserializeConversation(PacketReader pr)
        {
            return new ConversationModel()
            {
                Id = pr.ReadUInt64(),
                OwnerId = pr.ReadUInt64(),
                Name = pr.ReadUtf8String(pr.ReadUInt8())
            };
        }

        private static ParticipantModel DeserializeParticipant(PacketReader pr)
        {
            return new ParticipantModel
            {
                ParticipantId = pr.ReadUInt64(),
                // Jeszcze nie ustawiamy referencji do uczestnika.
                JoinTime = (long)pr.ReadUInt64(),
                IsAdministrator = pr.ReadUInt8()
            };
        }

        private static AccountModel DeserializeAccount(PacketReader pr)
        {
            return new AccountModel()
            {
                Id = pr.ReadUInt64(),
                Login = pr.ReadUtf8String(pr.ReadUInt8()),
                PublicKey = PublicKey.FromPacketReader(pr),
                IsBlocked = pr.ReadUInt8()
            };
        }
    }
}
