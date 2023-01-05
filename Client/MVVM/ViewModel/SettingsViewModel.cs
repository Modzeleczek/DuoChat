using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.ViewModel;

namespace Client.MVVM.ViewModel
{
    public class SettingsViewModel : WindowViewModel
    {
        #region Commands
        public RelayCommand LocalLogout { get; }
        public RelayCommand ToggleLanguage { get; }
        #endregion

        public SettingsViewModel(LocalUser user)
        {
            // nie trzeba robić obsługi WindowLoaded ani ustawiać pola window, jeżeli nie chcemy otwierać potomnych okien w tym viewmodelu
            LocalLogout = new RelayCommand(_ =>
            {
                OnRequestClose(new Status(2));
            });

            ToggleLanguage = new RelayCommand(_ =>
            {
                d.ToggleLanguage();
                new LocalUsersStorage().SetActiveLanguage(d.ActiveLanguageId);
            });
        }
    }
}
