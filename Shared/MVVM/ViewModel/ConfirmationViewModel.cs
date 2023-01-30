using Shared.MVVM.Core;
using Shared.MVVM.Model;

namespace Shared.MVVM.ViewModel
{
    public class ConfirmationViewModel : FormViewModel
    {
        public string Description { get; }

        protected ConfirmationViewModel(string title, string description,
            string cancelButtonText, string confirmButtonText)
        {
            Confirm = new RelayCommand(e => OnRequestClose(new Status(0)));

            Title = title;
            Description = description;
            CancelButtonText = cancelButtonText;
            ConfirmButtonText = confirmButtonText;
        }
    }
}
