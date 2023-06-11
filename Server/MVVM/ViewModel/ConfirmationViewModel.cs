using Server.MVVM.View.Windows;
using Shared.MVVM.Model;
using System.Windows;
using BaseConfirmationViewModel = Shared.MVVM.ViewModel.ConfirmationViewModel;

namespace Server.MVVM.ViewModel
{
    public class ConfirmationViewModel : BaseConfirmationViewModel
    {
        private ConfirmationViewModel(string title, string description,
            string cancelButtonText, string confirmButtonText) :
            base(title, description, cancelButtonText, confirmButtonText)
        { }

        public static Status ShowDialog(Window owner, string description, string title = null,
            string cancelButtonText = null, string confirmButtonText = null)
        {
            string finalTitle = title ?? "|Confirmation|";
            string finalCancButTxt = cancelButtonText ?? "|Cancel|";
            string finalConfButTxt = confirmButtonText ?? "|Confirm|";
            var vm = new ConfirmationViewModel(finalTitle, description,
                finalCancButTxt, finalConfButTxt);
            var win = new ConfirmationWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Status;
        }
    }
}
