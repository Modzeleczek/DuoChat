using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ClientToServer
{
    public class SearchUsers : Packet
    {
        #region Fields
        public const Codes CODE = Codes.SearchUsers;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            string loginFragment)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);
            var loginFragmentBytes = Encoding.UTF8.GetBytes(loginFragment);
            pb.Append((ulong)loginFragmentBytes.Length, 1);
            pb.Append(loginFragmentBytes);
            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr,
            out string loginFragment)
        {
            loginFragment = pr.ReadUtf8String(pr.ReadUInt8());
        }
    }

}
