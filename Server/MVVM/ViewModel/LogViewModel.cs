using Server.MVVM.ViewModel.Observables;
using Shared.MVVM.View.Windows;

namespace Server.MVVM.ViewModel
{
    public class LogViewModel : UserControlViewModel
    {
        private string _log;
        public string Log
        {
            get => _log;
            set { _log = value; OnPropertyChanged(); }
        }

        public LogViewModel(DialogWindow owner, Log log)
            : base(owner)
        {
            log.ChangedState += (stringBuilder) =>
                Log = stringBuilder.ToString();
        }
    }
}
