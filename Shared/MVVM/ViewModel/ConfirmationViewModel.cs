using Shared.MVVM.Core;
using Shared.MVVM.Model;

namespace Shared.MVVM.ViewModel
{
    public class ConfirmationViewModel : FormViewModel
    {
        public string Title { get; }
        public string Description { get; }
        public string CancellationButtonText { get; }
        public string ConfirmationButtonText { get; }

        protected ConfirmationViewModel(string title, string description,
            string cancellationButtonText, string confirmationButtonText)
        {
            Confirm = new RelayCommand(e => OnRequestClose(new Status(0)));

            Title = title;
            Description = description;
            CancellationButtonText = cancellationButtonText;
            ConfirmationButtonText = confirmationButtonText;
        }
    }
}
