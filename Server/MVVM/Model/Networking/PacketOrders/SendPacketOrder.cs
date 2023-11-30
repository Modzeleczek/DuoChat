using Shared.MVVM.Model;

namespace Server.MVVM.Model.Networking.PacketOrders
{
    public class SendPacketOrder : TimeoutableOrder
    {
        #region Properties
        public byte[] Packet { get; }
        #endregion

        public SendPacketOrder(byte[] packet)
        {
            Packet = packet;
        }
    }
}
