using Shared.MVVM.View.Localization;
using Shared.MVVM.Core;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class ViewModel : ObservableObject
    {
        #region Commands
        public RelayCommand WindowLoaded { get; protected set; }
        #endregion

        protected Window window;
        protected readonly Translator d = Translator.Instance;

        protected void Error(string description) =>
            AlertViewModel.ShowDialog(window, description);
    }
}
