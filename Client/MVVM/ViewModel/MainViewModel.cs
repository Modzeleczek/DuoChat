﻿using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        #region Commands
        private RelayCommand send;
        public RelayCommand Send
        {
            get => send;
            private set { send = value; OnPropertyChanged(); }
        }

        public RelayCommand WindowLoaded { get; }

        private RelayCommand close;
        public RelayCommand Close
        {
            get => close;
            private set { close = value; OnPropertyChanged(); }
        }

        private RelayCommand openSettings;
        public RelayCommand OpenSettings
        {
            get => openSettings;
            private set { openSettings = value; OnPropertyChanged(); }
        }
        #endregion

        #region Properties
        private Account account;
        public Account Account
        {
            get => account;
            private set { account = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Server> servers;
        public ObservableCollection<Server> Servers
        {
            get => servers;
            private set { servers = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Friend> friends;
        public ObservableCollection<Friend> Friends
        {
            get => friends;
            private set { friends = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Message> messages;
        public ObservableCollection<Message> Messages
        {
            get => messages;
            private set { messages = value; OnPropertyChanged(); }
        }

        private Server selectedServer;
        public Server SelectedServer
        {
            get { return selectedServer; }
            set { selectedServer = value; OnPropertyChanged(); }
        }

        private Friend selectedFriend;
        public Friend SelectedFriend
        {
            get { return selectedFriend; }
            set { selectedFriend = value; OnPropertyChanged(); }
        }

        private string messageContent;
        public string MessageContent
        {
            get { return messageContent; }
            set { messageContent = value; OnPropertyChanged(); }
        }
        #endregion
        
        public MainViewModel()
        {
            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                var mainWindow = (Window)windowLoadedE;

                Servers = new ObservableCollection<Server>();
                Messages = new ObservableCollection<Message>();
                Friends = new ObservableCollection<Friend>();

                Send = new RelayCommand(o =>
                {
                    Messages.Add(new Message
                    {
                        Content_ = MessageContent,
                        FirstMessage = false
                    });
                    MessageContent = "";
                });

                Close = new RelayCommand(e =>
                {
                    // przed faktycznym zamknięciem MainWindow, co powoduje zakończenie programu
                });
                OpenSettings = new RelayCommand(_ =>
                {
                    var vm = new SettingsViewModel();
                    var win = new SettingsWindow(mainWindow, vm);
                    vm.RequestClose += (s, e) => win.Close();
                    win.ShowDialog();
                    if (vm.Status.Code == 2) // wylogowanie
                        ShowLocalUsersDialog(mainWindow);
                });

                ShowLocalUsersDialog(mainWindow);
            });
        }

        private void ShowLocalUsersDialog(Window owner)
        {
            Reset();
            var vm = new LocalUsersViewModel();
            var win = new LocalUsersWindow(owner, vm);
            vm.RequestClose += (s, e) => win.Close();
            win.ShowDialog();
            Reset();
            Set();
            // jeżeli użytkownik się zalogował, to vm.Status.Code == 0
            if (vm.Status.Code != 0) // jeżeli użytkownik zamknął okno bez zalogowania się
                Application.Current.Shutdown();
        }

        private void Reset()
        {
            var lu = LoggedUser.Instance;
            Account = new Account { Nickname = lu.LocalName };
            Servers.Clear();
            Messages.Clear();
            Friends.Clear();
        }

        private void Set()
        {
            Servers.Add(new Server { IPAddress = "127.0.0.1", Name = "lokalny1" });
            Servers.Add(new Server { IPAddress = "127.0.0.2", Name = "lokalny2" });

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
        }
    }
}
