using Client.MVVM.Core;
using Client.MVVM.Model;

namespace Client.MVVM.ViewModel
{
    public class SettingsViewModel : DialogViewModel
    {
        #region Commands
        public RelayCommand LocalLogout { get; }
        #endregion

        public SettingsViewModel()
        {
            // nie trzeba robić obsługi WindowLoaded ani ustawiać pola window, jeżeli nie chcemy otwierać potomnych okien w tym viewmodelu
            LocalLogout = new RelayCommand(e =>
            {
                var lu = LoggedUser.Instance;
                lu.LocalName = null;
                lu.LocalPassword = null;
                OnRequestClose(new Status(1));
            });
        }
    }
}
