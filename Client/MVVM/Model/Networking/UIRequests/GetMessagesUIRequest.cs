using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking.Packets.ClientToServer.Message;
using System.Text;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class GetMessagesUIRequest : UIRequest
    {
        #region Properties
        public GetMessages.Filter Filter { get; }
        #endregion

        public GetMessagesUIRequest(GetMessages.Filter filter)
        {
            Filter = filter;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("GetMessages");
            sb.AppendFormat($"\n\tConversationId: {Filter.ConversationId}");
            sb.AppendFormat($"\n\tFindNewest: {Filter.FindNewest}");
            sb.AppendFormat($"\n\tMessageId: {Filter.MessageId}");
            return sb.ToString();
        }
    }
}
