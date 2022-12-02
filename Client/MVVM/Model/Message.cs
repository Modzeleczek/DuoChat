using System;
using System.Collections.Generic;

namespace Client.MVVM.Model
{
    public class Message
    {
        public string PlainContent { get; set; }
        public DateTime SendTime { get; set; }
        public DateTime ReceiveTime { get; set; }
        public DateTime DisplayTime { get; set; }
        // TODO: czas edycji i edytowanie wiadomości
        public bool IsDeleted { get; set; }
        public List<Attachment> Attachments { get; set; }
    }
}
