using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient.Participation
{
    public class AddedYouAsParticipant : Packet
    {
        #region Classes
        public class User
        {
            public ulong Id { get; set; }
            public string Login { get; set; } = null!;
            public PublicKey PublicKey { get; set; } = null!;
            // public byte IsBlocked { get; set; }
        }

        public class Participation
        {
            public long JoinTime { get; set; }
            public byte IsAdministrator { get; set; }
            public User Participant { get; set; } = null!;
        }

        public class Conversation
        {
            public ulong Id { get; set; }
            public string Name { get; set; } = null!;
            public User Owner { get; set; } = null!;
            public Participation[] Participations { get; set; } = null!;
        }

        public class YourParticipation
        {
            public long JoinTime { get; set; }
            public byte IsAdministrator { get; set; }
            public Conversation Conversation { get; set; } = null!;
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.AddedYouAsParticipant;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            YourParticipation yourParticipation)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            SerializeYourParticipation(ref pb, yourParticipation);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        private static void SerializeYourParticipation(ref PacketBuilder pb,
            YourParticipation participation)
        {
            pb.Append((ulong)participation.JoinTime, 8);
            pb.Append(participation.IsAdministrator, 1);
            SerializeConversation(ref pb, participation.Conversation);
        }

        private static void SerializeConversation(ref PacketBuilder pb, Conversation conversation)
        {
            pb.Append(conversation.Id, ID_SIZE);
            byte[] nameBytes = Encoding.UTF8.GetBytes(conversation.Name);
            // if (nameBytes.Length > 255) throw
            pb.Append((ulong)nameBytes.Length, 1);
            pb.Append(nameBytes);
            SerializeUser(ref pb, conversation.Owner);

            pb.Append((ulong)conversation.Participations.Length, 1);
            foreach (var p in conversation.Participations)
                SerializeParticipation(ref pb, p);
        }

        private static void SerializeUser(ref PacketBuilder pb, User user)
        {
            pb.Append(user.Id, ID_SIZE);
            byte[] loginBytes = Encoding.UTF8.GetBytes(user.Login);
            // if (loginBytes.Length > 255) throw
            pb.Append((ulong)loginBytes.Length, 1);
            pb.Append(loginBytes);
            pb.Append(user.PublicKey.ToBytes());
            // pb.Append(owner.IsBlocked, 1);
        }

        private static void SerializeParticipation(ref PacketBuilder pb, Participation participation)
        {
            pb.Append((ulong)participation.JoinTime, 8);
            pb.Append(participation.IsAdministrator, 1);
            SerializeUser(ref pb, participation.Participant);
        }

        public static void Deserialize(PacketReader pr,
            out YourParticipation yourParticipation)
        {
            yourParticipation = DeserializeYourParticipation(pr);
        }

        private static YourParticipation DeserializeYourParticipation(PacketReader pr)
        {
            return new YourParticipation
            {
                JoinTime = (long)pr.ReadUInt64(),
                IsAdministrator = pr.ReadUInt8(),
                Conversation = DeserializeConversation(pr)
            };
        }

        private static Conversation DeserializeConversation(PacketReader pr)
        {
            var conversation = new Conversation
            {
                Id = pr.ReadUInt64(),
                Name = pr.ReadUtf8String(pr.ReadUInt8()),
                Owner = DeserializeUser(pr),
                Participations = new Participation[pr.ReadUInt8()]
            };

            for (int p = 0; p < conversation.Participations.Length; ++p)
                conversation.Participations[p] = DeserializeParticipation(pr);

            return conversation;
        }

        private static User DeserializeUser(PacketReader pr)
        {
            return new User
            {
                Id = pr.ReadUInt64(),
                Login = pr.ReadUtf8String(pr.ReadUInt8()),
                PublicKey = PublicKey.FromPacketReader(pr),
                // IsBlocked = pr.ReadUInt8()
            };
        }

        private static Participation DeserializeParticipation(PacketReader pr)
        {
            return new Participation
            {
                JoinTime = (long)pr.ReadUInt64(),
                IsAdministrator = pr.ReadUInt8(),
                Participant = DeserializeUser(pr)
            };
        }
    }
}
