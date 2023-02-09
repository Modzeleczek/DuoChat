namespace Server.MVVM.ViewModel
{
    public class ViewModel : Shared.MVVM.ViewModel.ViewModel
    {
        protected void Alert(string description) => AlertViewModel.ShowDialog(window, description);
    }
}
