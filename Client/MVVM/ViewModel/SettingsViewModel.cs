using Client.MVVM.Model;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.Results;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class SettingsViewModel : WindowViewModel
    {
        #region Commands
        public RelayCommand LocalLogout { get; }
        public RelayCommand SwitchToEnglish { get; }
        public RelayCommand SwitchToPolish { get; }
        public RelayCommand ToggleTheme { get; }
        #endregion

        public enum Operations { LocalLogout }

        public SettingsViewModel(Storage storage)
        {
            // nie trzeba robić obsługi WindowLoaded ani ustawiać pola window, jeżeli nie chcemy otwierać potomnych okien w tym viewmodelu
            LocalLogout = new RelayCommand(_ =>
            {
                OnRequestClose(new Success(Operations.LocalLogout));
            });

            SwitchToEnglish = new RelayCommand(_ =>
            {
                d.SwitchToEnglish();
                storage.SetActiveLanguage((int)d.ActiveLanguage);
            });

            SwitchToPolish = new RelayCommand(_ =>
            {
                d.SwitchToPolish();
                storage.SetActiveLanguage((int)d.ActiveLanguage);
            });

            ToggleTheme = new RelayCommand(_ =>
            {
                var app = (App)Application.Current;
                app.ToggleTheme();
                storage.SetActiveTheme((int)app.ActiveTheme);
            });
        }
    }
}
