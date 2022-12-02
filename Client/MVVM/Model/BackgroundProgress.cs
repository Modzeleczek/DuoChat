using System.ComponentModel;

namespace Client.MVVM.Model
{
    public class BackgroundProgress
    {
        public BackgroundWorker worker;
        public DoWorkEventArgs args;
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
        public bool Cancel { set => args.Cancel = value; }
        public object Result { set => args.Result = value; }

        public BackgroundProgress(BackgroundWorker worker, DoWorkEventArgs args)
        {
            this.worker = worker;
            this.args = args;
            FineProgress = 0;
            CoarseProgress = 0;
        }

        private void UpdateWorkerProgress() =>
            worker.ReportProgress((int)
                // cp / cmax + fp / fmax * 1 / cmax = (cp + fp / fmax) / cmax
                (((CoarseProgress + FineProgress / FineMax) / CoarseMax) * 100.0));
    }
}
