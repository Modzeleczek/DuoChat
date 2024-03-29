using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel.Results;
using System.ComponentModel;
using System.Windows;

namespace Shared.MVVM.ViewModel.LongBlockingOperation
{
    public class ProgressBarViewModel : WindowViewModel
    {
        #region Properties
        private double progress;
        public double Progress
        {
            get => progress;
            /* Progress ma setter z powiadamianiem widoku (view) o zmianie (OnPropertyChanged),
            ponieważ jest edytowalne przez BackgroundWorkera i musi być odświeżane w GUI */
            set { progress = value; OnPropertyChanged(); }
        }

        private string description = null!;
        public string Description
        {
            get => description;
            set { description = d[value]; OnPropertyChanged(); }
        }

        private bool cancelable;
        public bool Cancelable
        {
            get => cancelable;
            set { cancelable = value; OnPropertyChanged(); }
        }

        private bool progressBarVisible;
        public bool ProgressBarVisible
        {
            get => progressBarVisible;
            set { progressBarVisible = value; OnPropertyChanged(); }
        }
        #endregion

        #region Fields
        private readonly BackgroundWorker worker;
        #endregion

        private ProgressBarViewModel(Work work, string description, bool cancelable,
            bool progressBarVisible)
        {
            /* potrzebne, jeżeli chcemy pojawiać alerty nad oknem postępu
            (ProgressBarWindow) */
            WindowLoaded = new RelayCommand(e => window = (DialogWindow)e!);
            /* worker.CancelAsync tylko ustawia worker.cancellationPending na true;
            getterem do worker.cancellationPending jest worker.CancellationPending */
            worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            Close = new RelayCommand(e =>
            {
                if (!worker.CancellationPending)
                {
                    worker.CancelAsync();
                    Description = "|Cancelling...|";
                    Cancelable = false; // deaktywujemy przycisk anulowania
                }
            });
            DoWorkEventHandler doWork = (worker, args) =>
            {
                work(new ProgressReporter((BackgroundWorker)worker!, args));
            };
            worker.DoWork += doWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            Description = description;
            Cancelable = cancelable;
            ProgressBarVisible = progressBarVisible;
        }

        protected void BeginWorking()
        {
            worker.RunWorkerAsync();
        }

        // wywoływane co wywołanie worker.ReportProgress
        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            Progress = e.ProgressPercentage;
        }

        // wywoływane po wyjściu z handlera DoWork poprzez return lub wyjątek
        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            /* if (e.Error != null) // wystąpił błąd
            else if (e.Cancelled) // użytkownik anulował
            else // zakończono powodzeniem

            u nas we wszystkich 3 przypadkach (błąd, anulowanie, powodzenie)
            informacja dla wywołującego viewmodelu jest w e.Result */

            var result = (Result)e.Result!;
            if (result is Failure failure) // wystąpił błąd
                Alert(failure.Reason.Message);
            // if (e.Result is Cancellation) // użytkownik anulował
            // if (e.Result is Success) zakończono powodzeniem
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
    }
}
