using Server.MVVM.Model;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking;
using System;

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
                string keyString = GetPrimaryKey().ToString();
                return keyString + (IsAuthenticated ? $" {_login}" : string.Empty);
            }
        }

        private string? _login = null;
        private bool IsAuthenticated => !(_login is null);

        public bool HasDisabledInteraction { get; private set; } = false;
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
                throw new InvalidOperationException("Observable client has been authenticated.");

            _login = login;
            OnPropertyChanged(nameof(DisplayedName));
        }

        public void DisableInteraction()
        {
            if (HasDisabledInteraction)
                throw new InvalidCastException("Observable client has disabled interaction.");

            HasDisabledInteraction = true;
            OnPropertyChanged(nameof(HasDisabledInteraction));
        }
    }
}
