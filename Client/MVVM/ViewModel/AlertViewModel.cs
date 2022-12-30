using Client.MVVM.View.Localization;
using Client.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class AlertViewModel : DialogViewModel
    {
        public string Title { get; }
        public string Description { get; }
        public string ButtonText { get; }

        public AlertViewModel(string title, string description, string buttonText)
        {
            Title = title;
            Description = description;
            ButtonText = buttonText;
        }

        public static void ShowDialog(Window owner, string description, string title = null,
            string buttonText = null)
        {
            var d = ClientTranslator.Instance;
            string finalTitle = title ?? d["Alert"];
            string finalButTxt = buttonText ?? d["OK"];
            var vm = new AlertViewModel(finalTitle, description, finalButTxt);
            var win = new AlertWindow(owner, vm);
            vm.RequestClose += (sender, args) => win.Close();
            win.ShowDialog();
        }
    }
}
