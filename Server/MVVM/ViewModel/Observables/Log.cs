using System;
using System.Text;

namespace Server.MVVM.ViewModel.Observables
{
    public class Log
    {
        #region Properties
        private StringBuilder _stringBuilder = new StringBuilder();
        #endregion

        #region Events
        public event Action<StringBuilder> ChangedState;
        #endregion

        public void Append(string text)
        {
            /* Append jest wywoływane tylko w wątku UI albo przez inne wątki
            w dispatcherze UI (czyli w zasadzie też w wątku UI), więc nie
            trzeba go synchronizować dedykowanym monitor lockiem. */
            _stringBuilder.Append($"{DateTime.UtcNow}: {text}\n");
            ChangedState(_stringBuilder);
        }
    }
}
