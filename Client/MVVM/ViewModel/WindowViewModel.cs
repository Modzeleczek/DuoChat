using BaseWindowViewModel = Shared.MVVM.ViewModel.WindowViewModel;

namespace Client.MVVM.ViewModel
{
    public class WindowViewModel : BaseWindowViewModel
    {
        protected void Alert(string description) =>
            AlertViewModel.ShowDialog(window, description);
    }
}
