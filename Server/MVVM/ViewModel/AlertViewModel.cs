using Server.MVVM.View.Windows;
using System.Windows;
using BaseAlertViewModel = Shared.MVVM.ViewModel.AlertViewModel;

namespace Server.MVVM.ViewModel
{
    public class AlertViewModel : BaseAlertViewModel
    {
        private AlertViewModel(string title, string description, string buttonText) :
            base(title, description, buttonText) { }

        public static void ShowDialog(Window owner, string description, string title = null,
            string buttonText = null)
        {
            string finalTitle = title ?? "|Alert|";
            string finalButTxt = buttonText ?? "|OK|";
            var vm = new AlertViewModel(finalTitle, description, finalButTxt);
            var win = new AlertWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
        }
    }
}
