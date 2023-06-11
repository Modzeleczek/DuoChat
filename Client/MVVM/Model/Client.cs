using Shared.MVVM.Core;
using Shared.MVVM.Model;
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

        public Status Connect(Server server)
        {
            Status status = null;
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
                return new Status(0); // 0
            }
            catch (OperationCanceledException)
            {
                status = new Status(-1, null, "|Server connection timed out.|"); // -1
            }
            catch (SocketException)
            {
                status = new Status(-2, null, "|No response from the server.|"); // -2
                // dokładna przyczyna braku połączenia jest w SocketException.Message
            }
            catch (Exception)
            {
                status = new Status(-3, null, "|Error occured while| " +
                    "|connecting to the server.|"); // -3
            }
            /* System.ArgumentNullException - nie może wystąpić, bo walidujemy adres IP
            System.ArgumentOutOfRangeException - nie może wystąpić, bo walidujemy port
            System.ObjectDisposedException - nie może wystąpić, bo tworzymy nowy,
            niezdisposowany obiekt TcpClient */
            _socket.Close();
            return status;
        }

        private void Process()
        {
            Status status = null;
            try
            {
                var stream = _socket.GetStream();
                while (true)
                {
                    lock (_socket) if (_disconnectRequested) break;
                }
                status = new Status(0);
            }
            catch (Exception ex)
            {
                status = new Status(-1, null, "|No translation:| " + ex.Message);
            }
            finally
            {
                _socket.Close();
                IsConnected = false;
                Disconnected?.Invoke(status);
            }
        }

        public Status Disconnect()
        {
            Status ret = null;
            Callback disconnectHandler = (status) => ret = status;
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
