using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using System.ComponentModel;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class ProgressBarViewModel : DialogViewModel
    {
        #region Commands
        public RelayCommand Cancel { get; protected set; }
        #endregion

        private BackgroundWorker worker;
        private double progress;
        public double Progress
        {
            get => progress;
            set { progress = value; OnPropertyChanged(); }
        }
        public string Description { get; }
        public bool Cancelable { get; }

        public ProgressBarViewModel(DoWorkEventHandler work, string description, bool cancelable)
        {
            Cancel = new RelayCommand(e => worker.CancelAsync());
            worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            worker.DoWork += work;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            Description = description;
            Cancelable = cancelable;
        }

        public void BeginWorking()
        {
            worker.RunWorkerAsync();
        }

        // wywoływane co wywołanie worker.ReportProgress
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Progress = e.ProgressPercentage;
        }

        // wywoływane po wyjściu z handlera DoWork poprzez return lub wyjątek
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var status = (Status)e.Result;
            /* if (e.Error != null) // wystąpił błąd
            else if (e.Cancelled) // użytkownik anulował
            else // zakończono powodzeniem */
            if (status.Code < 0) Error(status.Message);
            OnRequestClose(status); // we wszystkich 3 przypadkach (błąd, anulowanie, powodzenie) informacja dla wywołującego viewmodelu jest w statusie
        }

        public static Status ShowDialog(Window owner, string operationDescriptionText,
            bool cancelable, DoWorkEventHandler work)
        {
            var vm = new ProgressBarViewModel(work, operationDescriptionText, cancelable);
            var win = new ProgressBarWindow(owner, vm);
            // zapobiegamy ALT + F4 w oknie z progress barem
            CancelEventHandler cancelHandler = (sender, args) => args.Cancel = true;
            win.Closing += cancelHandler;
            vm.RequestClose += (sender, args) =>
            {
                win.Closing -= cancelHandler;
                win.Close();
            };
            vm.BeginWorking();
            win.ShowDialog();
            return vm.Status;
        }
    }
}
