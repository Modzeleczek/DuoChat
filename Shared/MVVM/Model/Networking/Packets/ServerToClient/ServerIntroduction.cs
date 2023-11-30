using Shared.MVVM.Model.Cryptography;
using System;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient
{
    public class ServerIntroduction : Packet
    {
        #region Fields
        public const Codes CODE = Codes.ServerIntroduction;
        #endregion

        public static byte[] Serialize(
            Guid guid,
            byte[] publicKeyBytes,
            ulong verificationToken)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(guid.ToByteArray());
            pb.Append(publicKeyBytes);
            pb.Append(verificationToken, TOKEN_SIZE);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr,
            out Guid guid,
            out PublicKey publicKey,
            out ulong verificationToken)
        {
            guid = pr.ReadGuid();
            publicKey = PublicKey.FromPacketReader(pr);
            verificationToken = pr.ReadUInt64();
        }
    }
}
