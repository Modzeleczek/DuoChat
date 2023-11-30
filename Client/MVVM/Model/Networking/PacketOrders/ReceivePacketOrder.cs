using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.PacketOrders
{
    public class ReceivePacketOrder : TimeoutableOrder
    {
        #region Classes
        public enum ExpectedPackets : byte
        {
            // Nie chcemy żadnego pakietu, tylko co najwyżej keep alive.
            KeepAlive = 0,
            NoSlots_Or_IPAlreadyBlocked_Or_ServerIntroduction,
            Authentication_Or_NoAuthentication_Or_AccountAlreadyBlocked,
            Notification
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
