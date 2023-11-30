namespace Shared.MVVM.Model.Networking.Packets.ServerToClient
{
    public class NoSlots : Packet
    {
        #region Fields
        public const Codes CODE = Codes.NoSlots;
        #endregion

        public static byte[] Serialize()
        {
            var pb = new PacketBuilder();
            pb.Append((byte)CODE, 1);
            return pb.Build();
        }
    }
}
