using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BaseClient = Shared.MVVM.Model.Networking.Client;

namespace Client.MVVM.Model
{
    public class Client : BaseClient
    {
        #region Fields
        private Server _server = null;
        #endregion

        public Client() { }

        public void Connect(IPAddress ipAddress, int port)
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
                    var task = _socket.ConnectAsync(ipAddress, port);
                    /* ustawiamy funkcję, która zostanie wykonana w momencie anulowania taska
                    obiektu CancellationTokenSource (czyli po czasie timeOut) */
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
                ResetFlags();
                /* zamiast poniższego taska można użyć tego:
                var receiver = Task.Factory.StartNew(ProcessReceive, TaskCreationOptions.LongRunning);
                var sender = Task.Factory.StartNew(ProcessSend, TaskCreationOptions.LongRunning);
                Task.Factory.ContinueWhenAll(new Task[] { receiver, sender }, Process); */
                _runner = Task.Factory.StartNew(Process, TaskCreationOptions.LongRunning);

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
            // wykonuje się, jeżeli złapiemy jakikolwiek wyjątek
            _socket.Close();
            throw error;
        }

        // Synchroniczne rozłączenie.
        public void Disconnect()
        {
            if (!IsConnected)
                throw new Error("|Client is not connected.|");
            /* czekamy na zakończenie wątku (taska) obsługującego
            połączenie z serwerem */
            DisconnectAsync().Wait();
        }
    }
}
