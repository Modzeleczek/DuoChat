using System;
using System.Text;

namespace Server.MVVM.ViewModel.Observables
{
    public class Log
    {
        #region Properties
        private StringBuilder _stringBuilder = new StringBuilder();
        #endregion

        #region Fields
        // TODO: przenieść do nadklasy SharedState
        private object _syncRoot = new object();
        #endregion

        #region Events
        public event Action<StringBuilder> ChangedState;
        #endregion

        public void Append(string text)
        {
            lock (_syncRoot)
            {
                _stringBuilder.Append($"{DateTime.UtcNow}: {text}\n");
                ChangedState(_stringBuilder);
            }
        }
    }
}
