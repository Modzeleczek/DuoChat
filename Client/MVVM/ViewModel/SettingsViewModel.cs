using Client.MVVM.Model.BsonStorages;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class SettingsViewModel : WindowViewModel
    {
        #region Commands
        public RelayCommand LocalLogout { get; }
        public RelayCommand ToggleLanguage { get; }
        public RelayCommand ToggleTheme { get; }
        #endregion

        public enum Operations { LocalLogout }

        public SettingsViewModel(LocalUser user)
        {
            // nie trzeba robić obsługi WindowLoaded ani ustawiać pola window, jeżeli nie chcemy otwierać potomnych okien w tym viewmodelu
            LocalLogout = new RelayCommand(_ =>
            {
                OnRequestClose(new Success(Operations.LocalLogout));
            });

            var lus = new LocalUsersStorage();
            ToggleLanguage = new RelayCommand(_ =>
            {
                d.ToggleLanguage();
                lus.SetActiveLanguage((int)d.ActiveLanguage);
            });

            ToggleTheme = new RelayCommand(_ =>
            {
                var app = (App)Application.Current;
                app.ToggleTheme();
                lus.SetActiveTheme((int)app.ActiveTheme);
            });
        }
    }
}
