using Shared.MVVM.Model;

namespace Client.MVVM.Model.Networking.PacketOrders
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
