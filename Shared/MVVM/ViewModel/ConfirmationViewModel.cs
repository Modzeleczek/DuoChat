using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;

namespace Shared.MVVM.ViewModel
{
    public class ConfirmationViewModel : FormViewModel
    {
        public string Description { get; }

        protected ConfirmationViewModel(string title, string description,
            string cancelButtonText, string confirmButtonText)
        {
            Confirm = new RelayCommand(e => OnRequestClose(new Success()));

            // setter w FormViewModel używa indexera Translatora do tłumaczenia
            Title = title;
            Description = d[description];
            CancelButtonText = cancelButtonText; // patrz Title
            ConfirmButtonText = confirmButtonText; // patrz Title
        }
    }
}
