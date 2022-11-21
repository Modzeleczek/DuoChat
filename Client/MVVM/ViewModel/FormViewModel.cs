using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Windows;

namespace Client.MVVM.ViewModel
{
    public class FormViewModel : DialogViewModel
    {
        #region Commands
        public RelayCommand Cancel { get; protected set; }
        public RelayCommand Confirm { get; protected set; }
        #endregion

        protected FormViewModel()
        {
            Cancel = new RelayCommand(e =>
            {
                // domyślnie ustawiony przy konstrukcji viewmodelu status ma kod 1, ale OnRequestClose przyjmuje i nadpisuje status
                OnRequestClose(new Status(1));
            });
        }

        protected void Error(string text) => AlertWindow.BadDialog(window, text);
    }
}
