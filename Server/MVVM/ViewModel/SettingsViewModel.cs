using Shared.MVVM.Core;
using System;

namespace Server.MVVM.ViewModel
{
    public class SettingsViewModel : ViewModel
    {
        #region Commands
        public RelayCommand ToggleServer { get; }
        #endregion

        #region Properties
        private short _port;
        public short Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private int _capacity;
        public int Capacity
        {
            get => _capacity;
            set { _capacity = value; OnPropertyChanged(); }
        }

        private bool _serverStopped;
        public bool ServerStopped
        {
            get => _serverStopped;
            set { _serverStopped = value; OnPropertyChanged(); }
        }
        #endregion

        public event Action ServerStart;
        public event Action ServerStop;

        public SettingsViewModel()
        {
            ToggleServer = new RelayCommand(e =>
            {
                if (!_serverStopped)
                {
                    if (ServerStop != null)
                    {
                        ServerStop();
                        ServerStopped = true;
                    }
                }
                else
                {
                    if (ServerStart != null)
                    {
                        ServerStart();
                        ServerStopped = false;
                    }
                }
            });
        }
    }
}
