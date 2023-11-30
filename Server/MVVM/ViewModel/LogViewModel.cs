using Shared.MVVM.View.Localization;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using System;
using System.Collections.ObjectModel;

namespace Server.MVVM.ViewModel
{
    public class LogViewModel : UserControlViewModel, ILogger
    {
        public ObservableCollection<string> Lines { get; } =
            new ObservableCollection<string>();

        public LogViewModel(DialogWindow owner)
            : base(owner)
        { }

        public void Log(string message)
        {
            /* Append jest wywoływane tylko w wątku UI albo przez inne wątki
            w dispatcherze UI (czyli w zasadzie też w wątku UI), więc nie
            trzeba go synchronizować dedykowanym monitor lockiem. */
            var d = Translator.Instance;
            Lines.Add($"{DateTime.UtcNow}: {d[message]}");
        }
    }
}
