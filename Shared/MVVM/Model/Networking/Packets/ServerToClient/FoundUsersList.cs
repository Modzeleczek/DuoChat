using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Text;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient
{
    public class FoundUsersList : Packet
    {
        #region Classes
        public class User
        {
            public ulong Id { get; set; }
            public string Login { get; set; } = null!;
        }
        #endregion

        #region Fields
        public const Codes CODE = Codes.FoundUsersList;
        #endregion

        public static byte[] Serialize(PrivateKey senderPrivateKey, PublicKey receiverPublicKey,
            ulong tokenFromRemoteSeed,
            User[] users)
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            pb.Append(tokenFromRemoteSeed, TOKEN_SIZE);

            pb.Append((ulong)users.Length, 1);
            foreach (var user in users)
            {
                pb.Append(user.Id, 8);
                byte[] loginBytes = Encoding.UTF8.GetBytes(user.Login);
                // if (loginBytes.Length > 255) throw
                pb.Append((ulong)loginBytes.Length, 1);
                pb.Append(loginBytes);
            }

            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            return pb.Build();
        }

        public static void Deserialize(PacketReader pr,
            out User[] users)
        {
            byte usersCount = pr.ReadUInt8();
            users = new User[usersCount];
            for (int u = 0; u < usersCount; ++u)
                users[u] = new User()
                {
                    Id = pr.ReadUInt64(),
                    Login = pr.ReadUtf8String(pr.ReadUInt8())
                };
        }
    }
}
