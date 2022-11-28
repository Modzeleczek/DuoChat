using Client.MVVM.View.Windows;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class AlertViewModel : DialogViewModel
    {
        public string AlertText { get; }
        public string ButtonText { get; }

        public AlertViewModel(string alertText, string buttonText = "OK")
        {
            AlertText = alertText;
            ButtonText = buttonText;
        }

        public static void ShowDialog(Window owner, string alertText, string buttonText = "OK")
        {
            new AlertWindow(owner, new AlertViewModel(alertText, buttonText)).ShowDialog();
        }
    }
}
