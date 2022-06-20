using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.MVVM.Model
{
    internal class MessageModel
    {
        public string Nickname { get; set; }
        public string UsernameColor { get; set; }
        public string ImageSource { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
        public bool isNativeOrigin { get; set; }
        public bool? FirstMessage  { get; set; }

    }
}
