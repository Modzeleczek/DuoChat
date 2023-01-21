using Client.MVVM.View.Windows;
using Shared.MVVM.View.Localization;
using System.Windows;
using BaseAlertViewModel = Shared.MVVM.ViewModel.AlertViewModel;

namespace Client.MVVM.ViewModel
{
    public class AlertViewModel : BaseAlertViewModel
    {
        private AlertViewModel(string title, string description, string buttonText) :
            base(title, description, buttonText) { }

        public static void ShowDialog(Window owner, string description, string title = null,
            string buttonText = null)
        {
            var d = Translator.Instance;
            string finalTitle = title ?? d["Alert"];
            string finalButTxt = buttonText ?? d["OK"];
            var vm = new AlertViewModel(finalTitle, description, finalButTxt);
            var win = new AlertWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
        }
    }
}
