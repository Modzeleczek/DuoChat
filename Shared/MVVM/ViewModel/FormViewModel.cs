using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;

namespace Shared.MVVM.ViewModel
{
    public class FormViewModel : WindowViewModel
    {
        #region Commands
        /* Wszystkie pochodne klasy-liście (z których już nie dziedziczy żadna
        klasa) ustawiają Confirm, ale można zrobić TODO: przenieść confirm do
        klas-liści. */
        private RelayCommand _confirm = null!;
        public RelayCommand Confirm
        {
            get => _confirm;
            protected set { _confirm = value; OnPropertyChanged(); }
        }
        #endregion

        #region Properties
        private string _title = null!;
        public string Title
        {
            get => _title;
            set { _title = d[value]; OnPropertyChanged(); }
        }

        private string _cancelButtonText = null!;
        public string CancelButtonText
        {
            get => _cancelButtonText;
            set { _cancelButtonText = d[value]; OnPropertyChanged(); }
        }

        private string _confirmButtonText = null!;
        public string ConfirmButtonText
        {
            get => _confirmButtonText;
            set { _confirmButtonText = d[value]; OnPropertyChanged(); }
        }
        #endregion

        protected FormViewModel()
        {
            Close = new RelayCommand(e => OnRequestClose(new Cancellation()));
            Confirm = new RelayCommand(_ => OnRequestClose(new Success()));
            Title = "|Form|";
            CancelButtonText = "|Cancel|";
            ConfirmButtonText = "|Confirm|";
        }
    }
}
