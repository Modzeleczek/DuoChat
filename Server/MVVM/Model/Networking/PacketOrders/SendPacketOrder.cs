using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking.Packets;

namespace Server.MVVM.Model.Networking.PacketOrders
{
    public class SendPacketOrder : TimeoutableOrder
    {
        #region Properties
        public byte[] Packet { get; }
        public Packet.Codes Code { get; }
        public string? Reason { get; }
        #endregion

        public SendPacketOrder(byte[] packet, Packet.Codes code, string? reason)
        {
            Packet = packet;
            Code = code;
            Reason = reason;
        }
    }
}
