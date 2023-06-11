using BaseFormViewModel = Shared.MVVM.ViewModel.FormViewModel;

namespace Client.MVVM.ViewModel
{
    public class FormViewModel : BaseFormViewModel
    {
        protected void Alert(string description) =>
            AlertViewModel.ShowDialog(window, description);
    }
}
