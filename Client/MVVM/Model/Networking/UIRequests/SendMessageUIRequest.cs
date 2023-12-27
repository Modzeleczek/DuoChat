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
            sb.AppendFormat($"Message.ConversationId {Message.ConversationId}");

            var attachmentMetadatas = Message.AttachmentMetadatas;
            sb.AppendFormat($"\nMessage.AttachmentMetadatas.Length: {attachmentMetadatas.Length}");
            foreach (var attMetName in attachmentMetadatas.Select(attMet => attMet.Name))
                sb.AppendFormat($"\n\tAttachmentMetadata.Name: {attMetName}");

            var recipients = Message.Recipients;
            sb.AppendFormat($"\nMessage.Recipients.Length: {recipients.Length}");
            foreach (var rec in recipients)
            {
                sb.AppendFormat($"\n\tRecipient.ParticipantId: {rec.ParticipantId}");
                sb.AppendFormat($"\n\tRecipient.EncryptedContent: {rec.EncryptedContent.ToHexString()}");

                var attachments = rec.Attachments;
                sb.AppendFormat($"\n\tRecipient.Attachments.Length: {rec.Attachments.Length}");
                foreach (var attEncryptedContent in attachments.Select(att => att.EncryptedContent))
                    sb.AppendFormat("\n\t\tAttachment.EncryptedContent: " +
                        attEncryptedContent.ToHexString());
            }

            return sb.ToString();
        }
    }
}
