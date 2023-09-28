using Client.MVVM.Model;
using Shared.MVVM.Core;
using Shared.MVVM.View.Localization;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.Results;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class SettingsViewModel : WindowViewModel
    {
        #region Commands
        public RelayCommand LocalLogout { get; }
        public RelayCommand SwitchLanguage { get; }
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

            SwitchLanguage = new RelayCommand(par =>
            {
                int languageId = int.Parse((string)par);
                d.SwitchLanguage((Translator.Language)languageId);
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
