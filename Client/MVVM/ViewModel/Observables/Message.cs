using Shared.MVVM.Core;
using System;
using System.Collections.Generic;

namespace Client.MVVM.ViewModel.Observables
{
    public class Message : ObservableObject
    {
        public ulong Id { get; set; }

        public User Sender { get; set; } = null!;

        public string PlainContent { get; set; } = null!;

        public DateTime SendTime { get; set; }

        public DateTime ReceiveTime { get; set; }

        public DateTime DisplayTime { get; set; }

        // TODO: czas edycji i edytowanie wiadomości

        public bool IsDeleted { get; set; }

        public List<Attachment> Attachments { get; set; } = null!;
    }
}
