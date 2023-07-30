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
        private object _syncRoot = new object(); // przenieść do nadklasy SharedState
        #endregion

        #region Events
        public event Action<StringBuilder> ChangedState;
        #endregion

        public void Append(string text)
        {
            lock (_syncRoot)
            {
                _stringBuilder.Append(text);
                _stringBuilder.Append('\n');
                ChangedState(_stringBuilder);
            }
        }
    }
}
