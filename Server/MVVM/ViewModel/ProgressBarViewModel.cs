using Server.MVVM.View.Windows;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System.ComponentModel;
using System.Windows;
using BaseProgressBarViewModel = Shared.MVVM.ViewModel.LongBlockingOperation.ProgressBarViewModel;

namespace Server.MVVM.ViewModel
{
    public class ProgressBarViewModel : BaseProgressBarViewModel
    {
        protected ProgressBarViewModel(Work work, string description, bool cancelable,
            bool progressBarVisible) :
            base(work, description, cancelable, progressBarVisible) { }

        // wywoływane po wyjściu z handlera DoWork poprzez return lub wyjątek
        protected override void Worker_RunWorkerCompleted(object sender,
            RunWorkerCompletedEventArgs e)
        {
            var result = (Result)e.Result;
            if (result is Failure failure)
                Alert(failure.Reason.Message);
            OnRequestClose(result);
        }

        public static Result ShowDialog(Window owner, string operationDescriptionText,
            bool cancelable, Work work, bool progressBarVisible = true)
        {
            var vm = new ProgressBarViewModel(work, operationDescriptionText, cancelable,
                progressBarVisible);
            var win = new ProgressBarWindow(owner, vm);
            // zapobiegamy ALT + F4 w oknie z progress barem
            win.Closable = false;
            vm.RequestClose += () =>
            {
                win.Closable = true;
                win.Close();
            };
            vm.BeginWorking();
            win.ShowDialog();
            return vm.Result;
        }

        private void Alert(string description) => AlertViewModel.ShowDialog(window, description);
    }
}
