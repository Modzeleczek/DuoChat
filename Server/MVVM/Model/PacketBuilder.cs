using System.IO;

namespace Server.MVVM.Model
{
    public class PacketBuilder
    {
        private MemoryStream _ms = new MemoryStream();

        public PacketBuilder() { }

        public void WriteOpCode(byte opCode)
        {
            _ms.WriteByte(opCode);
        }

        public void WriteMessage(byte[] token)
        {
            _ms.Write(token, 0, token.Length);
        }

        public void Encrypt()
        {
            /* using (var hc = new HybridCryptosystem())
                hc.SetParameters() */
        }

        public byte[] ToBytes()
        {
            return _ms.ToArray();
        }
    }
}
