using System;
using System.Threading.Tasks;
using System.Threading;

namespace Shared.MVVM.Model
{
    public abstract class UIRequest : TimeoutableOrder
    {
        protected UIRequest(int millisecondsTimeout = Timeout.Infinite)
        {
            // Callback jest wspólny dla normalnej sytuacji i timeoutu.
            StartTimeoutTaskIfNeeded(millisecondsTimeout);
        }

        private void StartTimeoutTaskIfNeeded(int millisecondsTimeout)
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
                        OnTimeout();
                    /* Jeżeli TryMarkAsTimedOut zwróci false, to znaczy, że TryMarkAsDone
                    zostało wywołane przed timeoutem, ale już po zwróceniu sterowania
                    z Wait. Wówczas pomijamy timeout. */
                }
                // TryMarkAsDone zostało wywołane przed lub podczas Wait.
                catch (AggregateException e) when (e.InnerException is TaskCanceledException) { }
            });
        }

        protected virtual void OnTimeout() { }
    }
}
