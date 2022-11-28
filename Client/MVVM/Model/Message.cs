using System;
using System.Collections.Generic;

namespace Client.MVVM.Model
{
    public class Message
    {
        public string Nickname { get; set; }
        public string UsernameColor { get; set; }
        public string PlainContent { get; set; }
        public DateTime SendTime { get; set; }
        public DateTime ReceiveTime { get; set; }
        public DateTime DisplayTime { get; set; }
        public List<Attachment> Attachments { get; set; }
    }
}
