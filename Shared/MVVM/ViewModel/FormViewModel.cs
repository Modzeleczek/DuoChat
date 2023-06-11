using Shared.MVVM.Core;
using Shared.MVVM.Model;

namespace Shared.MVVM.ViewModel
{
    public class FormViewModel : WindowViewModel
    {
        #region Commands
        private RelayCommand _confirm;
        public RelayCommand Confirm
        {
            get => _confirm;
            protected set { _confirm = value; OnPropertyChanged(); }
        }
        #endregion

        #region Properties
        private string _title;
        public string Title
        {
            get => _title;
            set { _title = d[value]; OnPropertyChanged(); }
        }

        private string _cancelButtonText;
        public string CancelButtonText
        {
            get => _cancelButtonText;
            set { _cancelButtonText = d[value]; OnPropertyChanged(); }
        }

        private string _confirmButtonText;
        public string ConfirmButtonText
        {
            get => _confirmButtonText;
            set { _confirmButtonText = d[value]; OnPropertyChanged(); }
        }
        #endregion

        protected FormViewModel()
        {
            Close = new RelayCommand(e => OnRequestClose(new Status(1)));
            CancelButtonText = "|Cancel|";
        }
    }
}
