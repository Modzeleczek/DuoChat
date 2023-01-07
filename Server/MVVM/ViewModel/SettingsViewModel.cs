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
            private set { _serverStopped = value; OnPropertyChanged(); }
        }
        #endregion

        private Model.Server _server;

        public SettingsViewModel(Window owner, Model.Server server)
        {
            window = owner;

            _server = server;
            Callback startStopHandler = (_) => ServerStopped = !_server.Running;
            _server.Started += startStopHandler;
            _server.Stopped += startStopHandler;

            ToggleServer = new RelayCommand(_ =>
            {
                if (!_server.Running)
                {
                    if (!ParseIpAddress(out IPv4Address ipAddress)) return;
                    if (!ParsePort(out ushort port)) return;
                    if (!ParseCapacity(out int capacity)) return;
                    _server.BeginStart(ipAddress.BinaryRepresentation, port, Name, capacity);
                }
                else _server.BeginStop();
            });
        }

        private bool ParseIpAddress(out IPv4Address ipAddress)
        {
            ipAddress = null;
            if (!IPv4Address.TryParse(IpAddress, out IPv4Address value))
            {
                Alert(d["Invalid IP address format."]);
                return false;
            }
            ipAddress = value;
            return true;
        }

        private bool ParsePort(out ushort port)
        {
            port = 0;
            if (!ushort.TryParse(Port, out ushort value))
            {
                Alert(d["Invalid port format."]);
                return false;
            }
            port = value;
            return true;
        }

        private bool ParseCapacity(out int capacity)
        {
            capacity = 0;
            if (!int.TryParse(Capacity, out int value))
            {
                Alert(d["Invalid capacity format."]);
                return false;
            }
            if (value <= 0)
            {
                Alert(d["Capacity must be positive."]);
                return false;
            }
            capacity = value;
            return true;
        }
    }
}
