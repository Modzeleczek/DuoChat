using Client.MVVM.Model.Networking;
using Client.MVVM.Model.Networking.UIRequests;
using Client.MVVM.ViewModel.Observables;
using Client.MVVM.ViewModel.Observables.Messages;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Packets.ClientToServer.Message;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Client.MVVM.ViewModel
{
    public class ConversationViewModel : UserControlViewModel
    {
        #region Commands
        public RelayCommand SendDraft { get; }
        public RelayCommand OpenMessageRecipients { get; }
        public RelayCommand DownloadAttachment { get; }
        public RelayCommand GetMoreMessages { get; }
        #endregion

        #region Properties
        private Conversation? _conversation;
        public Conversation? Conversation
        {
            get => _conversation;
            set
            {
                if (value == Conversation)
                    return;

                _conversation = value;
                OnPropertyChanged();

                if (!(Conversation is null) && Conversation.Messages.Count == 0)
                    _client.Request(new GetMessagesUIRequest(new GetMessages.Filter
                    {
                        ConversationId = Conversation.Id,
                        FindNewest = 1,
                        MessageId = 0,
                        MaxMessageCount = MESSAGE_PAGE_SIZE
                    }));

                // TODO: zapisywać w Conversation pozycję scrolla i tutaj ją przywracać.
            }
        }
        #endregion

        #region Fields
        // Liczba wiadomości na stronie.
        private const uint MESSAGE_PAGE_SIZE = 10;
        private readonly ClientMonolith _client;
        #endregion

        public ConversationViewModel(DialogWindow owner, ClientMonolith client)
            : base(owner)
        {
            _client = client;

            SendDraft = new RelayCommand(_ =>
            {
                /* Konwersacja nie może być null, bo wtedy
                ConversationView jest ukryte w MainWindow. */
                if (string.IsNullOrEmpty(Conversation!.Draft.Content))
                    return;

                byte[] contentBytes = Encoding.UTF8.GetBytes(Conversation.Draft.Content);
                var plainAttachmentsContents = ListPlainAttachments();
                if (plainAttachmentsContents is null)
                    return;

                // Wysyłamy do wszystkich uczestników i do właściciela konwersacji (do nas samych też).
                var outRecipients = new SendMessage.Recipient[Conversation.Participations.Count + 1];
                int r = 0;
                foreach (var recipientObs in Conversation.Participations.Select(p => p.Participant)
                    .Append(Conversation.Owner))
                {
                    var outAttachments = new SendMessage.Attachment[Conversation.Draft.AttachmentPaths.Count];
                    int a = 0;
                    foreach (var plainAttachment in plainAttachmentsContents)
                        outAttachments[a++] = new SendMessage.Attachment
                        { EncryptedContent = Encrypt(plainAttachment, recipientObs.PublicKey) };

                    outRecipients[r++] = new SendMessage.Recipient
                    {
                        AccountId = recipientObs.Id,
                        EncryptedContent = Encrypt(contentBytes, recipientObs.PublicKey),
                        Attachments = outAttachments
                    };
                }

                var outMessage = new SendMessage.Message
                {
                    ConversationId = Conversation.Id,
                    AttachmentMetadatas = Conversation.Draft.AttachmentPaths.Select(ap =>
                        new SendMessage.AttachmentMetadata { Name = Path.GetFileName(ap) }).ToArray(),
                    Recipients = outRecipients
                };

                client.Request(new SendMessageUIRequest(outMessage));
                Conversation.Draft.Content = string.Empty;
            });

            OpenMessageRecipients = new RelayCommand(obj =>
            {
                var messageObs = (Message)obj!;
                MessageRecipientsViewModel.ShowDialog(window!, client, messageObs);
            });

            DownloadAttachment = new RelayCommand(obj =>
            {
                var attachmentObs = (Attachment)obj!;
            });

            GetMoreMessages = new RelayCommand(_ =>
            {
                client.Request(new GetMessagesUIRequest(new GetMessages.Filter
                {
                    ConversationId = Conversation!.Id,
                    FindNewest = 0,
                    MessageId = Conversation.Messages[0].Id,
                    MaxMessageCount = MESSAGE_PAGE_SIZE
                }));
            });
        }

        private byte[] Encrypt(byte[] contentBytes, PublicKey publicKey)
        {
            var encryptingPb = new PacketBuilder();
            encryptingPb.Append(contentBytes);
            encryptingPb.Encrypt(publicKey);
            return encryptingPb.Build();
        }

        private byte[][]? ListPlainAttachments()
        {
            byte[][] attachments = new byte[Conversation!.Draft.AttachmentPaths.Count][];
            int i = 0;
            foreach (var path in Conversation.Draft.AttachmentPaths)
            {
                try
                {
                    // Jeżeli załącznik jest większy niż 2^16 = 65536 bajtów.
                    if (new FileInfo(path).Length > (1 << 16))
                    {
                        Alert($"|Attachment must be smaller than| {1 << 16} |bytes|.");
                        return null;
                    }

                    attachments[i++] = File.ReadAllBytes(path);
                }
                catch (Exception)
                {
                    Alert($"|Error occured while| |reading| |attachment| {path}");
                    return null;
                }
            }
            return attachments;
        }
    }
}
