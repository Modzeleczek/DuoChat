using Newtonsoft.Json;
using Server.MVVM.Model;
using Server.MVVM.Model.Networking;
using Server.MVVM.Model.Networking.UIRequests;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System;
using System.IO;
using System.Text;

namespace Server.MVVM.ViewModel
{
    public class SettingsViewModel : UserControlViewModel
    {
        #region Classes
        private sealed class SettingsJson
        {
            public string IpAddress = null!, Port = null!;
            public string? Guid, PrivateKey, Capacity;
        }
        #endregion

        #region Commands
        public RelayCommand ToggleServer { get; }

        public RelayCommand GenerateGuid { get; }

        public RelayCommand GeneratePrivateKey { get; }

        public RelayCommand Load { get; }
        public RelayCommand Save { get; }
        #endregion

        #region Properties
        private string? _guid;
        public string? Guid
        {
            get => _guid;
            set { _guid = value; OnPropertyChanged(); }
        }

        private string? _privateKey;
        public string? PrivateKey
        {
            get => _privateKey;
            set { _privateKey = value; OnPropertyChanged(); }
        }

        private string _ipAddress = null!;
        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }

        private string _port = null!;
        public string Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        private string? _capacity;
        public string? Capacity
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

        #region Fields
        private const string PATH = "settings.json";
        #endregion

        public SettingsViewModel(DialogWindow owner, ServerMonolith server, ILogger logger)
            : base(owner)
        {
            LoadFromFile();

            ToggleServer = new RelayCommand(_ =>
            {
                if (!ServerStopped)
                {
                    window!.SetEnabled(false);
                    server.Request(new StopServer(() => UIInvoke(() =>
                    {
                        ServerStopped = true;
                        logger.Log("|Server was stopped|.");
                        window!.SetEnabled(true);
                    })));
                    return;
                }

                if (!ParseGuid(out Guid guid)) return;
                if (!ParseIpAddress(out IPv4Address? ipAddress)) return;
                if (!ParsePort(out Port? port)) return;
                if (!ParseCapacity(out int capacity)) return;
                if (!ParsePrivateKey(out PrivateKey? privateKey)) return;

                try
                {
                    server.StartServer(guid, privateKey!, ipAddress!, port!, capacity);
                    ServerStopped = false;
                    logger.Log("|Server was started|.");
                }
                catch (Error e)
                {
                    Alert(e.Message);
                    throw;
                }
            });

            GenerateGuid = new RelayCommand(_ =>
            {
                var result = ConfirmationViewModel.ShowDialog(window!,
                    "|Do you want to generate a new GUID? Users may not trust a server changing its GUID without prior notice.|",
                    "|Generate GUID|", "|No|", "|Yes|");
                if (result is Success)
                    Guid = System.Guid.NewGuid().ToString();
            });

            GeneratePrivateKey = new RelayCommand(_ =>
            {
                var result = ConfirmationViewModel.ShowDialog(window!,
                    "|Do you want to generate a new private key? Server's public key is derived from private key and users may not trust a server changing it without prior notice.|",
                    "|Generate private key|", "|No|", "|Yes|");
                if (result is Success)
                {
                    var genRes = ProgressBarViewModel.ShowDialog(window!,
                        "|Private key generation|", true,
                        (reporter) => Shared.MVVM.Model.Cryptography.PrivateKey.Random(reporter));
                    if (!(genRes is Success success)) return; // anulowano (Cancellation)
                    PrivateKey = ((PrivateKey)success.Data!).ToString();
                }
            });

            Load = new RelayCommand(_ => LoadFromFile());
            Save = new RelayCommand(_ => SaveToFile());
        }

        private bool ParseGuid(out Guid guid)
        {
            guid = System.Guid.Empty;
            if (string.IsNullOrEmpty(Guid))
            {
                Alert("|Generate or enter a GUID.|");
                return false;
            }
            if (!System.Guid.TryParse(Guid, out Guid value))
            {
                Alert("|Invalid GUID format.|");
                return false;
            }
            guid = value;
            return true;
        }

        private bool ParsePrivateKey(out PrivateKey? privateKey)
        {
            privateKey = null;

            if (string.IsNullOrEmpty(PrivateKey))
            {
                Alert("|Generate or enter a private key.|");
                return false;
            }

            var result = ProgressBarViewModel.ShowDialog(window!,
                "|Private key validation|", true,
                (reporter) => Shared.MVVM.Model.Cryptography.PrivateKey.Parse(
                    reporter, PrivateKey));

            // anulowano lub błąd (błędy zostały już wyświetlone w ProgressBarViewModelu)
            if (!(result is Success success))
                return false;

            // powodzenie
            privateKey = (PrivateKey)success.Data!;
            return true;
        }

        private bool ParseIpAddress(out IPv4Address? ipAddress)
        {
            try
            {
                ipAddress = IPv4Address.Parse(IpAddress);
                return true;
            }
            catch (Error e)
            {
                e.Prepend("|Invalid IP address format.|");
                Alert(e.Message);
                ipAddress = null;
                return false;
            }
        }

        private bool ParsePort(out Port? port)
        {
            try
            {
                port = Shared.MVVM.Model.Networking.Port.Parse(Port);
                return true;
            }
            catch (Error e)
            {
                e.Prepend("|Invalid port format.|");
                Alert(e.Message);
                port = null;
                return false;
            }
        }

        private bool ParseCapacity(out int capacity)
        {
            capacity = 0;
            if (!int.TryParse(Capacity, out int value))
            {
                Alert("|Invalid capacity format.|");
                return false;
            }
            if (value <= 0)
            {
                Alert("|Capacity must be positive.|");
                return false;
            }
            capacity = value;
            return true;
        }

        private void LoadFromFile()
        {
            if (!File.Exists(PATH))
            {
                Alert("|Settings file does not exist. Default settings will be loaded.|");
                Guid = "5b8d0d10-d6d0-42af-8e35-6bcb4bf18872";
                PrivateKey = "59oF9YEte3yRp1IY2sFOR+PH6EWOCGCTanc8TPF/QSvCMi+6nCBDziJ7P" +
                    "2jKWPhka1F7VChMbIhN1QkEyW0NDK29R+6gHZr0dg3cJQMeLNzpyXD/KxaR8wPAolc" +
                    "W1012OcTdjsmRCQT6e8Fdm6hwKWNquYyMLNPIohiOtQB8jy4=;+UShzWdMi9leQFfM" +
                    "bvLirbgqlp728rvujtPLjYMDuWCyEsWPtxFd/KUsp75sQjH4hLf0404+JE68DpsbsE" +
                    "8OewL4ApIopRg8t+/UPkE7Pjz7oD0vJcC0E06qj/Mnjz+hbP/kxtnWEdP8mBluIDYr" +
                    "UO6hpJyUrSylvqDy5ZbqvnA=";
                IpAddress = "127.0.0.1";
                // według https://stackoverflow.com/a/38141340, powinniśmy używać portów <1024, 49151>
                Port = "13795";
                Capacity = "5";
                return;
            }
            string json = File.ReadAllText(PATH, Encoding.UTF8);
            var settings = JsonConvert.DeserializeObject<SettingsJson>(json);
            if (settings is null)
            {
                Alert("|Could not| |load settings file|.");
                return;
            }

            Guid = settings.Guid;
            PrivateKey = settings.PrivateKey;
            IpAddress = settings.IpAddress;
            Port = settings.Port;
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
                Capacity = Capacity
            };
            var json = JsonConvert.SerializeObject(settings);
            File.WriteAllText(PATH, json, Encoding.UTF8);
        }
    }
}
