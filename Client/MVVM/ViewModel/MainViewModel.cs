using Client.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    internal class MainViewModel : Window
    {
        public ObservableCollection<MessageModel> Messages {get; set;}
        public ObservableCollection<FriendModel> Friends { get; set; }


        public MainViewModel()
        {
            Messages = new ObservableCollection<MessageModel>();
            Friends = new ObservableCollection<FriendModel>();

            Messages.Add(new MessageModel
            {
                Username = "ProWoj",
                UsernameColor = "Red",
                ImageSource = "https://i.imgur.com/LZFX9Hx.png",
                Message = "Jaki priv?",
                Time = DateTime.Now,
                isNativeOrigin = false,
                FirstMessage = true
            });
            Friends.Add(new FriendModel
            {
                Username = "ProWoj",
                ImageSource = "https://i.imgur.com/LZFX9Hx.png",
                Messeges = Messages
            });
            Messages.Add(new MessageModel
            {
                Username = "RL9",
                UsernameColor = "Red",
                ImageSource = "https://i.imgur.com/bYBKzxY.png",
                Message = "Teraz to już przesadziła",
                Time = DateTime.Now,
                isNativeOrigin = false,
                FirstMessage = false
            });
            Friends.Add(new FriendModel
            {
                Username = "RL9",
                ImageSource = "https://i.imgur.com/bYBKzxY.png",
                Messeges = Messages
            });
        }

    }
}
