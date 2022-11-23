using Client.MVVM.Core;
using Client.MVVM.Model;

namespace Client.MVVM.ViewModel
{
    public class SettingsViewModel : DialogViewModel
    {
        #region Commands
        public RelayCommand LocalLogout { get; }
        #endregion

        public SettingsViewModel(LoggedUser user)
        {
            // nie trzeba robić obsługi WindowLoaded ani ustawiać pola window, jeżeli nie chcemy otwierać potomnych okien w tym viewmodelu
            LocalLogout = new RelayCommand(e =>
            {
                OnRequestClose(new Status(2));
            });
        }
    }
}
