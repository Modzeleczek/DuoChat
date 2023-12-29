using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking.Packets.ClientToServer.Message;
using System.Linq;
using System.Text;

namespace Client.MVVM.Model.Networking.UIRequests
{
    public class SendMessageUIRequest : UIRequest
    {
        #region Properties
        public SendMessage.Message Message { get; }
        #endregion

        public SendMessageUIRequest(SendMessage.Message message)
        {
            Message = message;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("SendMessage");
            sb.AppendFormat($"\n\tConversationId: {Message.ConversationId}");

            var attachmentMetadatas = Message.AttachmentMetadatas;
            sb.AppendFormat($"\n\tAttachmentMetadatas.Length: {attachmentMetadatas.Length}");
            foreach (var attMetName in attachmentMetadatas.Select(attMet => attMet.Name))
                sb.AppendFormat($"\n\t\tName: {attMetName}");

            var recipients = Message.Recipients;
            sb.AppendFormat($"\n\tRecipients.Length: {recipients.Length}");
            foreach (var rec in recipients)
            {
                sb.AppendFormat($"\n\t\tAccountId: {rec.AccountId}");
                sb.AppendFormat($"\n\t\tEncryptedContent: {rec.EncryptedContent.ToHexString()}");

                var attachments = rec.Attachments;
                sb.AppendFormat($"\n\t\tAttachments.Length: {rec.Attachments.Length}");
                foreach (var attEncryptedContent in attachments.Select(att => att.EncryptedContent))
                    sb.AppendFormat($"\n\t\t\tEncryptedContent: {attEncryptedContent.ToHexString()}");
            }

            return sb.ToString();
        }
    }
}
