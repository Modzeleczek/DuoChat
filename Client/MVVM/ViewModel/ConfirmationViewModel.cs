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
            string cancelButtonText, string confirmButtonText) :
            base(title, description, cancelButtonText, confirmButtonText) { }

        public static Status ShowDialog(Window owner, string description, string title = null,
            string cancelButtonText = null, string confirmButtonText = null)
        {
            var d = Translator.Instance;
            string finalTitle = title ?? d["Confirmation"];
            string finalCancButTxt = cancelButtonText ?? d["Cancel"];
            string finalConfButTxt = confirmButtonText ?? d["Confirm"];
            var vm = new ConfirmationViewModel(finalTitle, description,
                finalCancButTxt, finalConfButTxt);
            var win = new ConfirmationWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Status;
        }
    }
}
