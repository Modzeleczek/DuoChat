using Shared.MVVM.ViewModel.Results;
using System.ComponentModel;

namespace Shared.MVVM.ViewModel.LongBlockingOperation
{
    public class ProgressReporter
    {
        #region Properties
        private double fineProgress = 0;
        public double FineProgress
        {
            get => fineProgress;
            set { fineProgress = value; UpdateWorkerProgress(); }
        }

        public double FineMax { get; set; } = 1;

        private double coarseProgress = 0;
        public double CoarseProgress
        {
            get => coarseProgress;
            set { coarseProgress = value; UpdateWorkerProgress(); }
        }

        public double CoarseMax { get; set; } = 1;

        public bool CancellationPending { get => worker.CancellationPending; }

        public Result Result { set => args.Result = value; }
        #endregion

        #region Fields
        private BackgroundWorker worker;
        private DoWorkEventArgs args;
        #endregion

        public ProgressReporter(BackgroundWorker worker, DoWorkEventArgs args)
        {
            this.worker = worker;
            this.args = args;
            FineProgress = 0;
            CoarseProgress = 0;
        }

        // Do mockowania ProgressReportera.
        public ProgressReporter(DoWorkEventArgs args)
            : this(new BackgroundWorker { WorkerReportsProgress = true }, args) 
        { }

        private void UpdateWorkerProgress() =>
            worker.ReportProgress((int)
                // cp / cmax + fp / fmax * 1 / cmax = (cp + fp / fmax) / cmax
                (((CoarseProgress + FineProgress / FineMax) / CoarseMax) * 100.0));
    }
}
