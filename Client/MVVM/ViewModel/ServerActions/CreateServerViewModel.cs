using Shared.MVVM.Core;
using System.Windows.Controls;
using System;
using Shared.MVVM.Model.Networking;
using System.Collections.Generic;
using Shared.MVVM.ViewModel.Results;
using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Shared.MVVM.View.Windows;

namespace Client.MVVM.ViewModel.ServerActions
{
    public class CreateServerViewModel : ServerEditorViewModel
    {
        public CreateServerViewModel(Storage storage, LocalUserPrimaryKey loggedUserKey)
            : base(storage)
        {
            var currentWindowLoadedHandler = WindowLoaded!;
            WindowLoaded = new RelayCommand(e =>
            {
                currentWindowLoadedHandler.Execute(e);
                var win = (FormWindow)window!;
                win.AddTextField("|Name|");
                win.AddTextField("|IP address|");
                win.AddTextField("|Port|");
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls!;

                storage.GetLocalUser(loggedUserKey);

                /* TODO: coś takiego we wszystkich ...EditorViewModelach
                var logged = storage.GetLoggedLocalUser();
                if (logged is null || !logged.Equals(loggedUserKey))
                    throw new Error("User not logged.");

                Jeszcze lepszy pomysł, żeby nie powtarzać kodu w różnych
                EditorViewModelach:
                takie sprawdzanie powinno być w Storage,
                np. w ResolvePath i wyrzucanie Errora, jeżeli nie da się
                dojść po ścieżce. Np. w GetAccount(localUserKey, serverKey,
                accountLogin) można iść po łańcuchu localUser -> serverKey
                -> accountLogin i jeżeli nie istnieje któreś ogniwo, to
                informować wywołującego GetAccount (np. poprzez Error), że
                konto z loginem accountLogin nie istnieje albo poinformować,
                że nie istnieje brakujące ogniwo i dalsze.
                Klasa Storage powinna pilnować, żeby był zalogowany i
                odszyfrowany tylko 1 lokalny użytkownik jednocześnie. */

                /* Nie walidujemy, bo nazwa serwera jest przechowywana w polu
                w BSONie, a nie w SQLu, czyli nie jest podatna na SQL injection. */
                var name = ((TextBox)fields[0]).Text;

                if (!ParseIpAddress(((TextBox)fields[1]).Text, out IPv4Address? ipAddress))
                    return;

                if (!ParsePort(((TextBox)fields[2]).Text, out Port? port))
                    return;

                var serverKey = new ServerPrimaryKey(ipAddress!, port!);
                if (storage.ServerExists(loggedUserKey, serverKey))
                {
                    Alert(ServersStorage.AlreadyExistsMsg(serverKey));
                    return;
                }

                var newServer = new Observables.Server(serverKey)
                {
                    Name = name,
                    Guid = Guid.Empty,
                    PublicKey = null
                };
                try { storage.AddServer(loggedUserKey, newServer); }
                catch (Error e)
                {
                    e.Prepend("|Could not| |add| |server;D| |to user's database.|");
                    Alert(e.Message);
                    throw;
                }
                OnRequestClose(new Success(newServer));
            });
        }
    }
}
