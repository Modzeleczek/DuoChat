using Server.MVVM.View.Localization;
using Shared.MVVM.Core;

namespace Server.MVVM.ViewModel
{
    public class ViewModel : ObservableObject
    {
        protected readonly ServerTranslator d = ServerTranslator.Instance;
    }
}
