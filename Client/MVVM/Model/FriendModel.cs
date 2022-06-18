using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.MVVM.Model
{
    internal class FriendModel
    {   
        public string Username { get; set; }
        public string ImageSource { get; set; }
        public ObservableCollection<MessageModel> Messeges { get; set; }
        public string LastMessage => Messeges.Last().Message;
    }
}
