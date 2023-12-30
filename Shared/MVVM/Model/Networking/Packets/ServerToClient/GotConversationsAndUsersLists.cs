using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient
{
    public class GotConversationsAndUsersLists : Packet
    {
        #region Classes
        public class Conversation
        {
            public ulong Id { get; set; }
            public ulong OwnerId { get; set; }
            public string Name { get; set; } = null!;
            public uint NewMessagesCount { get; set; }
        }

        public class Participant
        {
            public ulong ParticipantId { get; set; }
            public long JoinTime { get; set; }
            public byte IsAdministrator { get; set; }
        }

        public class ConversationParticipation
        {
            public Conversation Conversation { get; set; } = null!;
            public Participant[] Participants { get; set; } = null!;
        }

        public class Account
        {
            public ulong Id { get; set; }
            public string Login { get; set; } = null!;
            public PublicKey PublicKey { get; set; } = null!;
            // public byte IsBlocked { get; set; }
        }

        public class Lists
        {
            public ConversationParticipation[] ConversationParticipants { get; set; } = null!;
            public Account[] Accounts { get; set; } = null!;
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.ConversationsAndUsersLists;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            Lists lists)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            SerializeLists(ref pb, lists);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        private static void SerializeLists(ref PacketBuilder pb, Lists lists)
        {
            pb.Append((ulong)lists.ConversationParticipants.Length, 1);
            foreach (var cp in lists.ConversationParticipants)
            {
                SerializeConversation(ref pb, cp.Conversation);

                pb.Append((ulong)cp.Participants.Length, 1);
                foreach (var p in cp.Participants)
                    SerializeParticipation(ref pb, p);
            }

            pb.Append((ulong)lists.Accounts.Length, 1);
            foreach (var a in lists.Accounts)
                SerializeAccount(ref pb, a);
        }

        private static void SerializeConversation(ref PacketBuilder pb, Conversation conversation)
        {
            pb.Append(conversation.Id, 8);
            pb.Append(conversation.OwnerId, 8);
            byte[] nameBytes = Encoding.UTF8.GetBytes(conversation.Name);
            // if (nameBytes.Length > 255) throw
            pb.Append((ulong)nameBytes.Length, 1);
            pb.Append(nameBytes);
            pb.Append(conversation.NewMessagesCount, 4);
        }

        private static void SerializeParticipation(ref PacketBuilder pb, Participant participant)
        {
            pb.Append(participant.ParticipantId, 8);
            pb.Append((ulong)participant.JoinTime, 8);
            pb.Append(participant.IsAdministrator, 1);
        }

        private static void SerializeAccount(ref PacketBuilder pb, Account account)
        {
            pb.Append(account.Id, 8);
            byte[] loginBytes = Encoding.UTF8.GetBytes(account.Login);
            // if (loginBytes.Length > 255) throw
            pb.Append((ulong)loginBytes.Length, 1);
            pb.Append(loginBytes);
            pb.Append(account.PublicKey.ToBytes());
            // pb.Append(account.IsBlocked, 1);
        }

        public static void Deserialize(PacketReader pr,
            out Lists lists)
        {
            lists = DeserializeLists(pr);
        }

        private static Lists DeserializeLists(PacketReader pr)
        {
            var lists = new Lists();

            lists.ConversationParticipants = new ConversationParticipation[pr.ReadUInt8()];
            for (int cp = 0; cp < lists.ConversationParticipants.Length; ++cp)
            {
                var conversation = DeserializeConversation(pr);

                var participants = new Participant[pr.ReadUInt8()];
                for (int p = 0; p < participants.Length; ++p)
                    participants[p] = DeserializeParticipant(pr);

                lists.ConversationParticipants[cp] = new ConversationParticipation
                {
                    Conversation = conversation,
                    Participants = participants
                };
            }

            lists.Accounts = new Account[pr.ReadUInt8()];
            for (int a = 0; a < lists.Accounts.Length; ++a)
                lists.Accounts[a] = DeserializeAccount(pr);

            return lists;
        }

        private static Conversation DeserializeConversation(PacketReader pr)
        {
            return new Conversation()
            {
                Id = pr.ReadUInt64(),
                OwnerId = pr.ReadUInt64(),
                Name = pr.ReadUtf8String(pr.ReadUInt8()),
                NewMessagesCount = pr.ReadUInt32()
            };
        }

        private static Participant DeserializeParticipant(PacketReader pr)
        {
            return new Participant
            {
                ParticipantId = pr.ReadUInt64(),
                // Jeszcze nie ustawiamy referencji do uczestnika.
                JoinTime = (long)pr.ReadUInt64(),
                IsAdministrator = pr.ReadUInt8()
            };
        }

        private static Account DeserializeAccount(PacketReader pr)
        {
            return new Account()
            {
                Id = pr.ReadUInt64(),
                Login = pr.ReadUtf8String(pr.ReadUInt8()),
                PublicKey = PublicKey.FromPacketReader(pr),
                // IsBlocked = pr.ReadUInt8()
            };
        }
    }
}
