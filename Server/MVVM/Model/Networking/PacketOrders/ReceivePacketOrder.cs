using Shared.MVVM.Model;

namespace Server.MVVM.Model.Networking.PacketOrders
{
    public class ReceivePacketOrder : TimeoutableOrder
    {
        #region Classes
        public enum ExpectedPackets : byte
        {
            KeepAlive = 0,
            ClientIntroduction,
            Request
        }
        #endregion

        #region Properties
        public ExpectedPackets ExpectedPacket { get; }
        #endregion

        public ReceivePacketOrder(ExpectedPackets expectedPacket)
        {
            ExpectedPacket = expectedPacket;
        }
    }
}
