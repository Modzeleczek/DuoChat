using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking;

namespace Server.MVVM.ViewModel.Observables
{
    public class ClientIPBlockObservable : ObservableObject
    {
        #region Properties
        private IPv4Address _ipAddress = null!;
        public IPv4Address IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }
        #endregion
    }
}
