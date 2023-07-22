using Shared.MVVM.View.Windows;

namespace Server.MVVM.ViewModel
{
    public class ViewModel : Shared.MVVM.ViewModel.ViewModel
    {
        protected ViewModel(DialogWindow owner)
        {
            window = owner;
        }

        protected void Alert(string description) => AlertViewModel.ShowDialog(window, description);
    }
}
