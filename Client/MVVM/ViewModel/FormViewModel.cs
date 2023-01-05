using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.ViewModel;

namespace Client.MVVM.ViewModel
{
    public class FormViewModel : WindowViewModel
    {
        #region Commands
        public RelayCommand Confirm { get; protected set; }
        #endregion

        protected FormViewModel()
        {
            Close = new RelayCommand(e => CancelHandler(e));
        }

        protected virtual void CancelHandler(object e)
        {
            // domyślnie ustawiony przy konstrukcji viewmodelu status ma kod 1, ale OnRequestClose przyjmuje i nadpisuje status
            OnRequestClose(new Status(1));
        }
    }
}
