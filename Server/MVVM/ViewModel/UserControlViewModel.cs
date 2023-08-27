using Shared.MVVM.View.Windows;

namespace Server.MVVM.ViewModel
{
    public class UserControlViewModel : Shared.MVVM.ViewModel.ViewModel
    {
        protected UserControlViewModel(DialogWindow owner)
        {
            window = owner;
        }
    }
}
