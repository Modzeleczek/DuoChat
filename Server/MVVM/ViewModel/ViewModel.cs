using Shared.MVVM.Core;
using Shared.MVVM.View.Localization;

namespace Server.MVVM.ViewModel
{
    public class ViewModel : ObservableObject
    {
        protected readonly Translator d = Translator.Instance;
    }
}
