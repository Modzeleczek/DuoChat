using Client.MVVM.Model;
using Client.MVVM.View.Converters;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class ConfirmationViewModel : FormViewModel
    {
        public string Title { get; }
        public string Description { get; }
        public string CancellationButtonText { get; }
        public string ConfirmationButtonText { get; }

        public ConfirmationViewModel(string title, string description,
            string cancellationButtonText, string confirmationButtonText)
        {
            Confirm = new RelayCommand(e => OnRequestClose(new Status(0)));

            Title = title;
            Description = description;
            CancellationButtonText = cancellationButtonText;
            ConfirmationButtonText = confirmationButtonText;
        }

        public static Status ShowDialog(Window owner, string description, string title = null,
            string cancellationButtonText = null, string confirmationButtonText = null)
        {
            var d = Strings.Instance;
            string finalTitle = title ?? d["Confirmation"];
            string finalCancButTxt = cancellationButtonText ?? d["Cancel"];
            string finalConfButTxt = confirmationButtonText ?? d["Confirm"];
            var vm = new ConfirmationViewModel(finalTitle, description,
                finalCancButTxt, finalConfButTxt);
            var win = new ConfirmationWindow(owner, vm);
            vm.RequestClose += (sender, args) => win.Close();
            win.ShowDialog();
            return vm.Status;
        }
    }
}
