using Client.MVVM.Core;
using System.Windows.Media;

namespace Client.MVVM.ViewModel
{
    public class AlertViewModel : ObservableObject
    {
        public string AlertText { get; }
        public int AlertFontSize { get; } = 16;
        public string ButtonText { get; } = "OK";
        public int ButtonFontSize { get; } = 16;
        public Color ButtonBackground { get; }

        public AlertViewModel(string text, Color buttonBackground)
        {
            AlertText = text;
            ButtonBackground = buttonBackground;
        }
    }
}
