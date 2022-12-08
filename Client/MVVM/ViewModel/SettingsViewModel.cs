using Client.MVVM.Model;
using Shared.MVVM.Core;

namespace Client.MVVM.ViewModel
{
    public class SettingsViewModel : DialogViewModel
    {
        #region Commands
        public RelayCommand LocalLogout { get; }
        #endregion

        public SettingsViewModel(LocalUser user)
        {
            // nie trzeba robić obsługi WindowLoaded ani ustawiać pola window, jeżeli nie chcemy otwierać potomnych okien w tym viewmodelu
            LocalLogout = new RelayCommand(e =>
            {
                OnRequestClose(new Status(2));
            });
        }
    }
}
