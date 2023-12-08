using Shared.MVVM.Model.Networking.Transfer.Transmission;

namespace Shared.MVVM.Model.Networking.Packets.ServerToClient
{
    public class IPAlreadyBlocked : Packet
    {
        #region Fields
        public const Codes CODE = Codes.IPAlreadyBlocked;
        #endregion

        public static byte[] Serialize()
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            return pb.Build();
        }
    }
}
