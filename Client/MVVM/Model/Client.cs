using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BaseClient = Shared.MVVM.Model.Networking.Client;

namespace Client.MVVM.Model
{
    public class Client : BaseClient
    {
        #region Classes
        public enum ClientState
        {
            ReadyToConnect = 0, Connected, ServerIntroduced,
            ClientIntroduced, ClientAuthenticated, EndedConnection
        }
        #endregion

        #region Fields
        // Obiekt obsługujący podłączonego klienta jest maszyną stanów.
        public ClientState State { get; set; } = ClientState.ReadyToConnect;
        public byte[] TokenCache { get; set; } = null;
        #endregion

        #region Events
        public event Action<Result> EndedConnection;
        public event Action<byte[]> ReceivedPacket;
        #endregion

        // Wywołujemy tylko raz, na początku programu.
        public Client()
        {
            _runner = Task.CompletedTask;
        }

        public void Connect(ServerPrimaryKey serverKey)
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
                    var task = _socket.ConnectAsync(
                        serverKey.IpAddress.ToIPAddress(), serverKey.Port.Value);
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

        protected override void OnEndedConnection(Result result)
        {
            // Wątek Client.Process
            EndedConnection(result);
        }

        protected override void OnReceivedPacket(byte[] packet)
        {
            // Wątek Client.ProcessHandle
            ReceivedPacket(packet);
        }
    }
}
