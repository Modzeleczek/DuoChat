using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Client.MVVM.Model
{
    public class Client
    {
        #region Properties
        public bool IsConnected { get; private set; } = false;
        #endregion

        #region Fields
        private TcpClient _socket = null;
        private Task _runner = null;
        private bool _disconnectRequested = false;
        private Server _server = null;
        #endregion

        #region Events
        public event Callback Disconnected;
        #endregion

        public Client() { }

        public void Connect(Server server)
        {
            Error error = null;
            // https://stackoverflow.com/a/43237063
            _socket = new TcpClient();
            var timeOut = TimeSpan.FromSeconds(2);
            var cancellationCompletionSource = new TaskCompletionSource<bool>();
            try
            {
                /* w obiekcie CancellationTokenSource tworzy się task "anulujący",
                który zostanie anulowany po czasie timeOut */
                using (var cts = new CancellationTokenSource(timeOut))
                {
                    // rozpoczynamy taska "łączącego", który łączy TcpClienta z serwerem
                    var task = _socket.ConnectAsync(server.IpAddress.ToIPAddress(), server.Port.Value);
                    /* ustawiamy funkcję, która zostanie wykonana w momencie anulowania taska obiektu CancellationTokenSource (czyli po czasie timeOut) */
                    using (cts.Token.Register(() => cancellationCompletionSource.TrySetResult(true)))
                    {
                        /* synchronicznie czekamy na zakończenie pierwszego z dwóch tasków:
                        łączącego lub anulującego; jeżeli pierwszy zakończy się nie task łączący,
                        ale anulujący, to wyrzucamy wyjątek */
                        var whenAny = Task.WhenAny(task, cancellationCompletionSource.Task);
                        whenAny.Wait();
                        if (whenAny.Result != task)
                            throw new OperationCanceledException(cts.Token);
                        /* jeżeli w tasku łączącym został wyrzucony wyjątek, to wyrzucamy
                        go w aktualnej metodzie, aby został obsłużony w catchach na dole */
                        // throw exception inside 'task' (if any)
                        if (task.Exception?.InnerException != null)
                            throw task.Exception.InnerException;
                    }
                }
                _server = server;
                _disconnectRequested = false;
                _runner = Task.Run(Process);
                IsConnected = true;
                return;
            }
            catch (OperationCanceledException e)
            {
                error = new Error(e, "|Server connection timed out.|");
            }
            catch (SocketException e)
            {
                error = new Error(e, "|No response from the server.|");
                // dokładna przyczyna braku połączenia jest w SocketException.Message
            }
            catch (Exception e)
            {
                error = new Error(e, "|Error occured while| " +
                    "|connecting to the server.|");
            }
            /* System.ArgumentNullException - nie może wystąpić, bo walidujemy adres IP
            System.ArgumentOutOfRangeException - nie może wystąpić, bo walidujemy port
            System.ObjectDisposedException - nie może wystąpić, bo tworzymy nowy,
            niezdisposowany obiekt TcpClient */
            _socket.Close();
            throw error;
        }

        private void Process()
        {
            Result result = null;
            try
            {
                var stream = _socket.GetStream();
                while (true)
                {
                    lock (_socket) if (_disconnectRequested) break;
                }
                result = new Success();
            }
            catch (Exception ex)
            {
                result = new Failure(ex, "|No translation:|");
            }
            finally
            {
                _socket.Close();
                IsConnected = false;
                Disconnected?.Invoke(result);
            }
        }

        public Result Disconnect()
        {
            Result ret = null;
            Callback disconnectHandler = (result) => ret = result;
            Disconnected += disconnectHandler;
            RequestDisconnect();
            // czekamy na zakończenie wątku (taska) obsługującego klienta
            _runner.Wait();
            Disconnected -= disconnectHandler;
            return ret;
        }

        public void RequestDisconnect()
        {
            if (!IsConnected) return;
            lock (_socket) _disconnectRequested = true;
        }
    }
}
