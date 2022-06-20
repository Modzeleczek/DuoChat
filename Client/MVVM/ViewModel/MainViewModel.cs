using Client.Core;
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
    internal class MainViewModel : ObservableObject
    {
        public ObservableCollection<MessageModel> Messages {get; set;}
        public ObservableCollection<FriendModel> Friends { get; set; }


        /* Commands */
        public RelayCommand SendCommand { get; set; }

        private FriendModel _selectedFriend;   

        public FriendModel SelectedFriend
        {
            get { return _selectedFriend; }
            set {
                _selectedFriend = value;
                OnPropertyChanged();
            }
        }


        private string _message;

        public string Message
        {
            get { return _message; }
            set { _message = value;
                OnPropertyChanged();
            }
        }



        public MainViewModel()
        {
            Messages = new ObservableCollection<MessageModel>();
            Friends = new ObservableCollection<FriendModel>();

            SendCommand = new RelayCommand(o => 
            {
                Messages.Add(new MessageModel
                {
                    Message = Message,
                    FirstMessage = false
                });
                Message = "";
            });

            Messages.Add(new MessageModel
            {
                Nickname = "ProWoj",
                UsernameColor = "Red",
                ImageSource = "https://i.imgur.com/LZFX9Hx.png",
                Message = "Jaki priv?",
                Time = DateTime.Now,
                isNativeOrigin = false,
                FirstMessage = true
            });
            Friends.Add(new FriendModel
            {
                Nickname = "ProWoj",
                ImageSource = "https://i.imgur.com/LZFX9Hx.png",
                Messages = Messages
            });
            Messages.Add(new MessageModel
            {
                Nickname = "RL9",
                UsernameColor = "Gray",
                ImageSource = "https://i.imgur.com/bYBKzxY.png",
                Message = "Teraz to już przesadziła",
                Time = DateTime.Now,
                isNativeOrigin = false,
                FirstMessage = false
            });
            Friends.Add(new FriendModel
            {
                Nickname = "RL9",
                ImageSource = "https://i.imgur.com/bYBKzxY.png",
                Messages = Messages
            });
        }

    }
}
