using Shared.MVVM.View.Windows;

namespace Shared.MVVM.ViewModel
{
    public class UserControlViewModel : ViewModel
    {
        protected UserControlViewModel(DialogWindow owner)
        {
            window = owner;
        }
    }
}
