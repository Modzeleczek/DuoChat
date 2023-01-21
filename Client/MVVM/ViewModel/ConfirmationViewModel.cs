using Client.MVVM.View.Windows;
using Shared.MVVM.Model;
using Shared.MVVM.View.Localization;
using System.Windows;
using BaseConfirmationViewModel = Shared.MVVM.ViewModel.ConfirmationViewModel;

namespace Client.MVVM.ViewModel
{
    public class ConfirmationViewModel : BaseConfirmationViewModel
    {
        private ConfirmationViewModel(string title, string description,
            string cancellationButtonText, string confirmationButtonText) :
            base(title, description, cancellationButtonText, confirmationButtonText) { }

        public static Status ShowDialog(Window owner, string description, string title = null,
            string cancellationButtonText = null, string confirmationButtonText = null)
        {
            var d = Translator.Instance;
            string finalTitle = title ?? d["Confirmation"];
            string finalCancButTxt = cancellationButtonText ?? d["Cancel"];
            string finalConfButTxt = confirmationButtonText ?? d["Confirm"];
            var vm = new ConfirmationViewModel(finalTitle, description,
                finalCancButTxt, finalConfButTxt);
            var win = new ConfirmationWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Status;
        }
    }
}
