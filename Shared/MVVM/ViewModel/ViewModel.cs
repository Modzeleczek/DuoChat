using Shared.MVVM.Core;
using Shared.MVVM.View.Localization;
using System.Windows;

namespace Shared.MVVM.ViewModel
{
    public class ViewModel : ObservableObject
    {
        protected Window window;
        protected readonly Translator d = Translator.Instance;
    }
}
