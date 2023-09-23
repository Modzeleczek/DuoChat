using Server.MVVM.Model;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking;

namespace Server.MVVM.ViewModel.Observables
{
    public class ClientObservable : ObservableObject
    {
        #region Properties
        // Klucz główny klienta jest tylko do odczytu.
        public IPv4Address IpAddress { get; }
        public Port Port { get; }

        public string DisplayedName
        {
            get
            {
                string keyString = GetPrimaryKey().ToString("{0}:{1}");
                if (!IsAuthenticated)
                    return keyString;
                else
                    return keyString + $" {_login}";
            }
        }

        private string _login = null;
        public bool IsAuthenticated => _login != null;
        #endregion

        public ClientObservable(ClientPrimaryKey key)
        {
            IpAddress = key.IpAddress;
            Port = key.Port;
        }

        public ClientPrimaryKey GetPrimaryKey()
        {
            return new ClientPrimaryKey(IpAddress, Port);
        }

        public void Authenticate(string login)
        {
            if (IsAuthenticated)
                throw new Error("|Observable client has already been authenticated|.");

            _login = login;
            OnPropertyChanged(nameof(DisplayedName));
        }
    }
}
