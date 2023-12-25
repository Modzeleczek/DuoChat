using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer.Participation
{
    public class DeleteParticipation : Packet
    {
        #region Classes
        public enum Errors : byte
        {
            YouNeitherConversationOwnerNorAdmin = 0,
            ParticipationNotExists = 1
        }

        public class Participation
        {
            public ulong ConversationId { get; set; }
            public ulong ParticipantId { get; set; }
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.DeleteParticipation;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            Participation participation)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            pb.Append(participation.ConversationId, ID_SIZE);
            pb.Append(participation.ParticipantId, ID_SIZE);

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr,
            out Participation participation)
        {
            participation = new Participation
            {
                ConversationId = pr.ReadUInt64(),
                ParticipantId = pr.ReadUInt64()
            };
        }
    }
}
