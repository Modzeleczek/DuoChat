using System;

namespace Client.MVVM.Model
{
    public class Message
    {
        public string Nickname { get; set; }
        public string UsernameColor { get; set; }
        public string ImageSource { get; set; }
        public string Content_ { get; set; }
        public DateTime Time { get; set; }
        public bool IsNativeOrigin { get; set; }
        public bool? FirstMessage { get; set; }
    }
}
