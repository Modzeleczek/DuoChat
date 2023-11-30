using System;
using System.Threading.Tasks;
using System.Threading;

namespace Shared.MVVM.Model
{
    public class UIRequest : TimeoutableOrder
    {
        #region Properties
        public object? Parameter { get; }
        public Action? Callback { get; }
        #endregion

        protected UIRequest(object? parameter, Action? callback, int millisecondsTimeout)
        {
            Parameter = parameter;
            // Callback jest wspólny dla normalnej sytuacji i timeoutu.
            Callback = callback;

            StartTimeoutTaskIfNeeded(millisecondsTimeout, callback);
        }

        private void StartTimeoutTaskIfNeeded(int millisecondsTimeout, Action? callback)
        {
            if (millisecondsTimeout == Timeout.Infinite)
                return;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    // Task.Delay można zcancelować.
                    Task.Delay(millisecondsTimeout, GetCancellationToken()).Wait();

                    if (TryMarkAsTimedOut())
                        // Wystąpił timeout.
                        callback?.Invoke();
                    /* Jeżeli TryMarkAsTimedOut zwróci false, to znaczy, że TryMarkAsDone
                    zostało wywołane przed timeoutem, ale już po zwróceniu sterowania
                    z Wait. Wówczas pomijamy timeout. */
                }
                // TryMarkAsDone zostało wywołane przed lub podczas Wait.
                catch (AggregateException e) when (e.InnerException is TaskCanceledException) { }
            });
        }
    }
}
