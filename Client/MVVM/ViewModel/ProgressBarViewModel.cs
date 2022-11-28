using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using System;
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

        public ProgressBarViewModel(DoWorkEventHandler work)
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
        }

        public void BeginWorking()
        {
            worker.RunWorkerAsync();
        }

        public class BackgroundWorkError : Exception
        {
            public BackgroundWorkError(string message) : base(message) { }
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

        public static Status ShowDialog(Window owner,
            string windowTitle, string operationDescriptionText, DoWorkEventHandler work)
        {
            var vm = new ProgressBarViewModel(work);
            var win = new ProgressBarWindow(owner, vm, windowTitle, operationDescriptionText);
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
