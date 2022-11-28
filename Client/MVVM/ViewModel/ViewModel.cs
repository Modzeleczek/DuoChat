using Client.MVVM.Core;
using Client.MVVM.View.Converters;
using Client.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class ViewModel : ObservableObject
    {
        #region Commands
        public RelayCommand WindowLoaded { get; protected set; }
        #endregion

        protected Window window;
        protected readonly Strings d = Strings.Instance;

        protected void Error(string alertText, string buttonText = "OK") =>
            AlertViewModel.ShowDialog(window, alertText, buttonText);
    }
}
