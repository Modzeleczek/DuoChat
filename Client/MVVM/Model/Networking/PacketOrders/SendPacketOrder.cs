using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking.Packets;

namespace Client.MVVM.Model.Networking.PacketOrders
{
    public class SendPacketOrder : TimeoutableOrder
    {
        #region Properties
        public byte[] Packet { get; }
        public Packet.Codes Code { get; }
        #endregion

        public SendPacketOrder(byte[] packet, Packet.Codes code)
        {
            Packet = packet;
            Code = code;
        }
    }
}
