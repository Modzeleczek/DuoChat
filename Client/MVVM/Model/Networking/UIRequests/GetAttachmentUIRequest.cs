using Shared.MVVM.Model;
using System.Text;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class GetAttachmentUIRequest : UIRequest
    {
        #region Properties
        public ulong AttachmentId { get; }
        #endregion

        public GetAttachmentUIRequest(ulong attachmentId)
        {
            AttachmentId = attachmentId;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("GetAttachment");
            sb.AppendFormat($"\n\tAttachmentId: {AttachmentId}");
            return sb.ToString();
        }
    }
}
