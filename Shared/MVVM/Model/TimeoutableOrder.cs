using System.Threading;

namespace Shared.MVVM.Model
{
    public abstract class TimeoutableOrder
    {
        #region Properties
        private readonly object _lock = new object();
        private readonly CancellationTokenSource _timeoutCTS = new CancellationTokenSource();
        private bool _isDoneOrTimedOut = false;
        #endregion

        public CancellationToken GetCancellationToken() => _timeoutCTS.Token;

        public bool TryMarkAsDone()
        {
            /* Zakładamy, że ta metoda będzie wywołana tylko raz
            na pojedynczym obiekcie TimeoutableOrder. */
            lock (_lock)
            {
                if (!_isDoneOrTimedOut)
                {
                    _isDoneOrTimedOut = true;
                    _timeoutCTS.Cancel();
                    return true;
                }
                return false;
            }
        }

        public bool TryMarkAsTimedOut()
        {
            // To samo założenie co w TryMarkAsDone.
            lock (_lock)
            {
                if (!_isDoneOrTimedOut)
                {
                    _isDoneOrTimedOut = true;
                    return true;
                }
                return false;
            }
        }
    }
}
