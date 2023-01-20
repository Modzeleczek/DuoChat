using Newtonsoft.Json;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;

namespace Server.MVVM.ViewModel
{
    public class SettingsViewModel : ViewModel
    {
        private class SettingsJson
        {
            public string Guid, PrivateKey, IpAddress, Port, Name, Capacity;
        }

        #region Commands
        public RelayCommand ToggleServer { get; }

        public RelayCommand GenerateGuid { get; }

        public RelayCommand GeneratePrivateKey { get; }

        public RelayCommand Load { get; }
        public RelayCommand Save { get; }
        #endregion

        #region Properties
        private string _guid;
        public string Guid
        {
            get => _guid;
            set { _guid = value; OnPropertyChanged(); }
        }

        private string _privateKey;
        public string PrivateKey
        {
            get => _privateKey;
            set { _privateKey = value; OnPropertyChanged(); }
        }

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

        private const string PATH = "settings.json";

        public SettingsViewModel(Window owner, Model.Server server)
        {
            window = owner;

            LoadFromFile();

            Callback startStopHandler = (_) => ServerStopped = !server.IsRunning;
            server.Started += startStopHandler;
            server.Stopped += startStopHandler;
            // server.Started += (_) => ServerStopped = false;
            // server.Stopped += (_) => ServerStopped = true;

            ToggleServer = new RelayCommand(_ =>
            {
                // if (!ServerStopped)
                if (server.IsRunning)
                {
                    server.RequestStop();
                    return;
                }
                if (!ParseGuid(out Guid guid)) return;
                if (!ParseIpAddress(out IPv4Address ipAddress)) return;
                if (!ParsePort(out int port)) return;
                if (!ParseCapacity(out int capacity)) return;
                if (!ParsePrivateKey(out PrivateKey privateKey)) return;
                server.Start(guid, privateKey, ipAddress.ToIPAddress(), port,
                    Name, capacity);
            });

            GenerateGuid = new RelayCommand(_ =>
            {
                var status = ConfirmationViewModel.ShowDialog(window,
                    d["Do you want to generate a new GUID? Users may not trust a server changing its GUID without prior notice."],
                    d["Generate GUID"], d["No"], d["Yes"]);
                if (status.Code == 0)
                    Guid = System.Guid.NewGuid().ToString();
            });

            GeneratePrivateKey = new RelayCommand(_ =>
            {
                var status = ConfirmationViewModel.ShowDialog(window,
                    d["Do you want to generate a new private key? Server's public key is derived from private key and users may not trust a server changing it without prior notice."],
                    d["Generate private key"], d["No"], d["Yes"]);
                if (status.Code == 0)
                    PrivateKey = new PrivateKey().ToString();
            });

            Load = new RelayCommand(_ => LoadFromFile());
            Save = new RelayCommand(_ => SaveToFile());
        }

        private bool ParseGuid(out Guid guid)
        {
            guid = System.Guid.Empty;
            if (string.IsNullOrEmpty(Guid))
            {
                Alert(d["Generate or enter a GUID."]);
                return false;
            }
            if (!System.Guid.TryParse(Guid, out Guid value))
            {
                Alert(d["Invalid GUID format."]);
                return false;
            }
            guid = value;
            return true;
        }

        private bool ParsePrivateKey(out PrivateKey privateKey)
        {
            privateKey = null;
            if (string.IsNullOrEmpty(PrivateKey))
            {
                Alert(d["Generate or enter a private key."]);
                return false;
            }
            var status = ProgressBarViewModel.ShowDialog(window,
                d["Private key validation"], true,
                (worker, args) => Shared.MVVM.Model.Cryptography.PrivateKey.TryParse(
                    new ProgressReporter((BackgroundWorker)worker, args),
                    PrivateKey));
            if (status.Code == 1) return false; // anulowano
            if (status.Code < 0)
                return false;
            privateKey = (PrivateKey)status.Data;
            return true;
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

        private bool ParsePort(out int port)
        {
            port = 0;
            if (!int.TryParse(Port, out int value))
            {
                Alert(d["Invalid port format."]);
                return false;
            }
            int min = IPEndPoint.MinPort, max = IPEndPoint.MaxPort;
            if (!(value >= min && value <= max))
            {
                Alert(d["Port must be in range"] + $" <{min}, {max}>.");
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

        private void LoadFromFile()
        {
            if (!File.Exists(PATH))
            {
                Alert(d["Settings file does not exist. Default settings will be loaded."]);
                Guid = "5b8d0d10-d6d0-42af-8e35-6bcb4bf18872";
                PrivateKey = "cm0iaUQj443FfN49ph9E9tFuOURkFrM6U8mya7bVclipEiYYucYIAkCMs4gz1sVgYF3TMNXDI2tW3essYROD22xMHRkQRZDy54LxaB8peto3DfSA7g1uW/l6kZhzQBB0QDhWjbxrfNV9vQCL1GhX3yPD7bFp1Hdb2ROJxXlB9ac=;VzcEpdIXqHpyn8+ol80vUkTX1LNHduUC/sCwNom9WH+ergMlBfEEcE7RlZ+dvdGC/Ji2elbnvJBZAGWH13MImUGMEGEoiphCrfGttbqTntoUpL34WRfC+ttxFRgmstCSQKQkuiAz+FqhM7QzesW49bTEH0tcwJf026QMvxEgqgk=";
                IpAddress = "127.0.0.1";
                // według https://stackoverflow.com/a/38141340, powinniśmy używać portów <1024, 49151>
                Port = "13795";
                Name = "Testowy serwer";
                Capacity = "5";
                return;
            }
            string json = File.ReadAllText(PATH, Encoding.UTF8);
            var settings = JsonConvert.DeserializeObject<SettingsJson>(json);
            Guid = settings.Guid;
            PrivateKey = settings.PrivateKey;
            IpAddress = settings.IpAddress;
            Port = settings.Port;
            Name = settings.Name;
            Capacity = settings.Capacity;
        }

        private void SaveToFile()
        {
            // jeżeli plik nie istnieje, to zostanie stworzony
            var settings = new SettingsJson
            {
                Guid = Guid,
                PrivateKey = PrivateKey,
                IpAddress = IpAddress,
                Port = Port,
                Name = Name,
                Capacity = Capacity
            };
            var json = JsonConvert.SerializeObject(settings);
            File.WriteAllText(PATH, json, Encoding.UTF8);
        }
    }
}
