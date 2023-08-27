using Shared.MVVM.View.Windows;
using System.Windows;

namespace Shared.MVVM.ViewModel
{
    public class AlertViewModel : WindowViewModel
    {
        public string Title { get; }
        public string Description { get; }
        public string ButtonText { get; }

        private AlertViewModel(string title, string description, string buttonText)
        {
            /* Nie zapisujemy window w WindowLoaded, bo z tego
            ViewModelu nie uruchamiamy potomnych okien. */

            Title = d[title];
            Description = d[description];
            ButtonText = d[buttonText];
        }

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
