using Client.Core;
using Client.MVVM.Model;
using System;
using System.Collections.ObjectModel;

namespace Client.MVVM.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private object currentView;
        public RelayCommand SwitchToSettings { get; set; }
        public object CurrentModeVM
        {
            get { return currentView; }
            set { currentView = value; OnPropertyChanged(nameof(CurrentModeVM)); }
        }



        public ObservableCollection<Server> Servers { get; }
        public ObservableCollection<Friend> Friends { get; }
        public ObservableCollection<Message> Messages { get; }

        public RelayCommand Send { get; }
        public RelayCommand WindowLoaded { get; }
        public RelayCommand Close { get; }

        private Server selectedServer;
        public Server SelectedServer
        {
            get { return selectedServer; }
            set { selectedServer = value; OnPropertyChanged(nameof(SelectedServer)); }
        }

        private Friend selectedFriend;
        public Friend SelectedFriend
        {
            get { return selectedFriend; }
            set { selectedFriend = value; OnPropertyChanged(nameof(SelectedFriend)); }
        }

        private string messageContent;
        public string MessageContent
        {
            get { return messageContent; }
            set { messageContent = value; OnPropertyChanged(nameof(MessageContent)); }
        }

        public MainViewModel()
        {


            Servers = new ObservableCollection<Server>();
            Messages = new ObservableCollection<Message>();
            Friends = new ObservableCollection<Friend>();

            var defaultVM = new MainViewModel();
            var settingsVM = new MenuSettingsViewModel();

            currentView = defaultVM;
            SwitchToSettings = new RelayCommand(o => { if (CurrentModeVM != settingsVM) CurrentModeVM = settingsVM; });
            Send = new RelayCommand(o => 
            {
                Messages.Add(new Message
                {
                    Content_ = MessageContent,
                    FirstMessage = false
                });
                MessageContent = "";
            });

            Servers.Add(new Server{ IPAddress = "127.0.0.1", Name = "lokalny1" });
            Servers.Add(new Server{ IPAddress = "127.0.0.2", Name = "lokalny2" });

            Messages.Add(new Message
            {
                Nickname = "ProWoj",
                UsernameColor = "Red",
                ImageSource = "https://i.imgur.com/LZFX9Hx.png",
                Content_ = "Jaki priv?",
                Time = DateTime.Now,
                IsNativeOrigin = false,
                FirstMessage = true
            });
            Friends.Add(new Friend
            {
                Nickname = "ProWoj",
                ImageSource = "https://i.imgur.com/LZFX9Hx.png",
                Messages = Messages
            });
            Messages.Add(new Message
            {
                Nickname = "RL9",
                UsernameColor = "Gray",
                ImageSource = "https://i.imgur.com/bYBKzxY.png",
                Content_ = "Teraz to już przesadziłaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                Time = DateTime.Now,
                IsNativeOrigin = false,
                FirstMessage = false
            });
            Friends.Add(new Friend
            {
                Nickname = "RL9",
                ImageSource = "https://i.imgur.com/bYBKzxY.png",
                Messages = Messages
            });

            Close = new RelayCommand(e =>
            {
                // przed faktycznym zamknięciem MainWindow, co powoduje zakończenie programu
            });
        }
    }
}
