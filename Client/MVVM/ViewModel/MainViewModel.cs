﻿using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class MainViewModel : ViewModel
    {
        #region Commands
        private RelayCommand send;
        public RelayCommand Send
        {
            get => send;
            private set { send = value; OnPropertyChanged(); }
        }

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
        private ObservableCollection<Server> servers;
        public ObservableCollection<Server> Servers
        {
            get => servers;
            set { servers = value; OnPropertyChanged(); }
        }

        private Server selectedServer;
        public Server SelectedServer
        {
            get => selectedServer;
            set
            {
                selectedServer = value;
                SelectedAccount = null;
                Accounts.Clear();
                if (value != null)
                {
                    var accCnt = rng.Next(0, 5);
                    for (int i = 0; i < accCnt; ++i)
                        Accounts.Add(Account.Random(rng));
                }
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Account> accounts;
        public ObservableCollection<Account> Accounts
        {
            get => accounts;
            set { accounts = value; OnPropertyChanged(); }
        }

        private Account selectedAccount;
        public Account SelectedAccount
        {
            get => selectedAccount;
            set
            {
                selectedAccount = value;
                SelectedConversation = null;
                Conversations.Clear();
                if (value != null)
                {
                    var cnvCnt = rng.Next(0, 5);
                    for (int i = 0; i < cnvCnt; ++i)
                        Conversations.Add(Conversation.Random(rng));
                }
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Conversation> conversations;
        public ObservableCollection<Conversation> Conversations
        {
            get => conversations;
            set { conversations = value; OnPropertyChanged(); }
        }

        private Conversation selectedConversation;
        public Conversation SelectedConversation
        {
            get => selectedConversation;
            set { selectedConversation = value; OnPropertyChanged(); }
        }

        private string writtenMessage;
        public string WrittenMessage
        {
            get => writtenMessage;
            set { writtenMessage= value; OnPropertyChanged(); }
        }
        #endregion

        private LocalUser loggedUser = null;
        private Random rng = new Random();
        
        public MainViewModel()
        {
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                window = (Window)windowLoadedE;

                Send = new RelayCommand(o =>
                {
                    if (SelectedConversation == null ||
                        WrittenMessage.Length == 0) return;
                    var rnd = Message.Random(rng);
                    rnd.PlainContent = WrittenMessage;
                    SelectedConversation.Messages.Add(rnd);
                    WrittenMessage = "";
                });

                Close = new RelayCommand(e =>
                {
                    // przed faktycznym zamknięciem MainWindow, co powoduje zakończenie programu
                });
                OpenSettings = new RelayCommand(_ =>
                {
                    var vm = new SettingsViewModel(loggedUser);
                    var win = new SettingsWindow(window, vm);
                    vm.RequestClose += (s, e) => win.Close();
                    win.ShowDialog();
                    if (vm.Status.Code == 2) // wylogowanie
                    {
                        Clear();
                        var logSta = LocalLoginViewModel.Dialog(window, loggedUser, true);
                        if (logSta.Code != 0) return;
                        var curPas = (SecureString)((dynamic)logSta.Data).Password;

                        lus.SetLogged(false);

                        var pc = new PasswordCryptography();
                        var encSta = ProgressBarViewModel.ShowDialog(window,
                            d["Encrypting user's database."], true,
                            (worker, args) =>
                            pc.EncryptDirectory(new BackgroundProgress((BackgroundWorker)worker, args),
                                loggedUser.GetDatabase().DirectoryPath,
                                pc.ComputeDigest(curPas, loggedUser.DBSalt),
                                loggedUser.DBInitializationVector));
                        curPas.Dispose();
                        if (encSta.Code == 1)
                        {
                            Error(d["User's database encryption canceled. Not logging out."]);
                            return;
                        }
                        else if (encSta.Code != 0)
                        {
                            Error(encSta.Message);
                            return;
                        }
                        loggedUser = null;
                        while (ShowLocalUsersDialog(lus).Code < 0) ;
                    }
                });

                Servers = new ObservableCollection<Server>();
                Accounts = new ObservableCollection<Account>();
                Conversations = new ObservableCollection<Conversation>();
                Randomize();
                var getLogSta = lus.GetLogged();
                if (getLogSta.Code == 0)
                {
                    var userName = (string)getLogSta.Data;
                    var getSta = lus.Get(userName);
                    if (getSta.Code == 0)
                    {
                        loggedUser = (LocalUser)getSta.Data;
                        return;
                    }
                    Error(d["Logged user does not exist."]);
                }
                while (ShowLocalUsersDialog(lus).Code < 0) ;
            });
        }

        private Status ShowLocalUsersDialog(LocalUsersStorage lus)
        {
            var vm = new LocalUsersViewModel();
            var win = new LocalUsersWindow(window, vm);
            vm.RequestClose += (s, e) => win.Close();
            win.ShowDialog();
            var status = vm.Status;
            // jeżeli użytkownik się zalogował, to vm.Status.Code == 0
            if (status.Code != 0) // jeżeli użytkownik zamknął okno bez zalogowania się
                Application.Current.Shutdown();
            else
            {
                var dat = (dynamic)status.Data;
                var curPas = (SecureString)dat.Password;
                var user = (LocalUser)dat.LoggedUser;
                var db = user.GetDatabase();

                var pc = new PasswordCryptography();
                status = ProgressBarViewModel.ShowDialog(window,
                    d["Decrypting user's database."], true,
                    (worker, args) =>
                    pc.DecryptDirectory(new BackgroundProgress((BackgroundWorker)worker, args),
                        db.DirectoryPath,
                        pc.ComputeDigest(curPas, user.DBSalt),
                        user.DBInitializationVector));
                curPas.Dispose();
                if (status.Code == 1)
                    Error(d["User's database decryption canceled. Logging out."]);
                else if (status.Code != 0)
                    Error(status.Message);
                // decSta.Code == 0
                lus.SetLogged(true, user.Name);
                loggedUser = user;
                Randomize();
            }
            return status;
        }
        
        private void Clear()
        {
            SelectedConversation = null;
            SelectedAccount = null;
            SelectedServer = null;
            Conversations.Clear();
            Accounts.Clear();
            Servers.Clear();
        }

        private void Randomize()
        {
            Servers.Clear();
            Accounts.Clear();
            Conversations.Clear();
            var srvCnt = rng.Next(0, 5);
            for (int i = 0; i < srvCnt; ++i)
                Servers.Add(Server.Random(rng));
        }
    }
}
