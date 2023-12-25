using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient.Participation
{
    public class AddedParticipation : Packet
    {
        #region Classes
        public class Participant
        {
            public ulong Id { get; set; }
            public string Login { get; set; } = null!;
            public PublicKey PublicKey { get; set; } = null!;
            // public byte IsBlocked { get; set; }
        }

        public class Participation
        {
            public ulong ConversationId { get; set; }
            public long JoinTime { get; set; }
            public byte IsAdministrator { get; set; }
            public Participant Participant { get; set; } = null!;
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.AddedParticipation;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            Participation participation)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            SerializeParticipation(ref pb, participation);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        private static void SerializeParticipation(ref PacketBuilder pb, Participation participation)
        {
            pb.Append(participation.ConversationId, ID_SIZE);
            pb.Append((ulong)participation.JoinTime, 8);
            pb.Append(participation.IsAdministrator, 1);
            SerializeParticipant(ref pb, participation.Participant);
        }

        private static void SerializeParticipant(ref PacketBuilder pb, Participant account)
        {
            pb.Append(account.Id, ID_SIZE);
            byte[] loginBytes = Encoding.UTF8.GetBytes(account.Login);
            // if (loginBytes.Length > 255) throw
            pb.Append((ulong)loginBytes.Length, 1);
            pb.Append(loginBytes);
            pb.Append(account.PublicKey.ToBytes());
            // pb.Append(account.IsBlocked, 1);
        }

        public static void Deserialize(PacketReader pr,
            out Participation participation)
        {
            participation = DeserializeParticipation(pr);
        }

        private static Participation DeserializeParticipation(PacketReader pr)
        {
            return new Participation
            {
                ConversationId = pr.ReadUInt64(),
                JoinTime = (long)pr.ReadUInt64(),
                IsAdministrator = pr.ReadUInt8(),
                Participant = DeserializeParticipant(pr)
            };
        }

        private static Participant DeserializeParticipant(PacketReader pr)
        {
            return new Participant()
            {
                Id = pr.ReadUInt64(),
                Login = pr.ReadUtf8String(pr.ReadUInt8()),
                PublicKey = PublicKey.FromPacketReader(pr),
                // IsBlocked = pr.ReadUInt8()
            };
        }
    }
}
