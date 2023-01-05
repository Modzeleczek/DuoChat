using Shared.MVVM.Core;
using Shared.MVVM.Model;
using System.Windows;

namespace Server.MVVM.ViewModel
{
    public class SettingsViewModel : ViewModel
    {
        #region Commands
        public RelayCommand ToggleServer { get; }
        #endregion

        #region Properties

        private string _ipAddress;
        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }

        private string _port;
        public string Port
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

        private string _capacity;
        public string Capacity
        {
            get => _capacity;
            set { _capacity = value; OnPropertyChanged(); }
        }

        private bool _serverStopped = true;
        public bool ServerStopped
        {
            get => _serverStopped;
            set { _serverStopped = value; OnPropertyChanged(); }
        }
        #endregion

        private Model.Server _server;

        public SettingsViewModel(Window owner, Model.Server server)
        {
            window = owner;
            _server = server;
            ToggleServer = new RelayCommand(_ =>
            {
                if (!_server.Started)
                {
                    if (!IPv4Address.TryParse(IpAddress, out IPv4Address ipAddress))
                    {
                        Alert(d["Invalid IP address format."]);
                        return;
                    }
                    if (!ushort.TryParse(Port, out ushort port))
                    {
                        Alert(d["Invalid port format."]);
                        return;
                    }
                    if (!uint.TryParse(Capacity, out uint capacity))
                    {
                        Alert(d["Invalid capacity format."]);
                        return;
                    }
                    _server.Start(ipAddress.BinaryRepresentation, port, Name, capacity);
                    ServerStopped = false;
                }
                else
                {
                    _server.Stop();
                    ServerStopped = true;
                }
            });
        }
    }
}
