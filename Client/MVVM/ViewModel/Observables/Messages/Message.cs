using Shared.MVVM.Core;
using System;
using System.Collections.ObjectModel;

namespace Client.MVVM.ViewModel.Observables.Messages
{
    public class Message : ObservableObject
    {
        #region Classes
        public enum Type { Received = 0, Sent = 1 }
        #endregion

        #region Properties
        public int ReceivedOrSent { get; }

        public ulong Id { get; init; }

        // Id nadawcy posiadane przez serwer.
        public ulong? RemoteSenderId { get; init; } = null!;

        // Nadawca wyświetlany w GUI.
        private User? _sender = null!;
        public User? Sender
        {
            get => _sender;
            set
            {
                _sender = value;
                OnPropertyChanged();
            }
        }

        public string PlainContent { get; init; } = null!;

        public DateTime SendTime { get; init; }

        public Recipient[] Recipients { get; }

        public ObservableCollection<Attachment> Attachments { get; init; } = null!;

        // IsDeleted
        #endregion

        public Message(Type type, Recipient[] recipients)
        {
            ReceivedOrSent = (int)type;
            Recipients = recipients;
        }
    }
}
