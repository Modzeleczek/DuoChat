using Shared.MVVM.Model.Cryptography;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient
{
    public class RequestError : Packet
    {
        #region Fields
        public const Codes CODE = Codes.RequestError;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            Codes faultyOperationCode,
            byte errorCode)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);
            pb.Append((byte)faultyOperationCode, 1);
            pb.Append(errorCode, 1);
            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr,
            out ulong token,
            out Codes faultyOperationCode,
            out byte errorCode)
        {
            token = pr.ReadUInt64();
            faultyOperationCode = (Codes)pr.ReadUInt8();
            errorCode = pr.ReadUInt8();
        }
    }
}
