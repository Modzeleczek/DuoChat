using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel.Results;
using System.Windows;

namespace Shared.MVVM.ViewModel
{
    public class ConfirmationViewModel : FormViewModel
    {
        public string Description { get; }

        private ConfirmationViewModel(string title, string description,
            string cancelButtonText, string confirmButtonText)
        {
            /* Nie zapisujemy window w WindowLoaded, bo z tego
            ViewModelu nie uruchamiamy potomnych okien. */

            Confirm = new RelayCommand(e => OnRequestClose(new Success()));

            // setter w FormViewModel używa indexera Translatora do tłumaczenia
            Title = title;
            Description = d[description];
            CancelButtonText = cancelButtonText; // patrz Title
            ConfirmButtonText = confirmButtonText; // patrz Title
        }

        public static Result ShowDialog(Window owner, string description, string? title = null,
            string? cancelButtonText = null, string? confirmButtonText = null)
        {
            string finalTitle = title ?? "|Confirmation|";
            string finalCancButTxt = cancelButtonText ?? "|Cancel|";
            string finalConfButTxt = confirmButtonText ?? "|Confirm|";
            var vm = new ConfirmationViewModel(finalTitle, description,
                finalCancButTxt, finalConfButTxt);
            var win = new ConfirmationWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Result;
        }
    }
}
