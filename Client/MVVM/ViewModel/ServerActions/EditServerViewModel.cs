using Client.MVVM.Model;
using Shared.MVVM.Core;
using System.Windows.Controls;
using System;
using Shared.MVVM.Model.Networking;
using System.Collections.Generic;
using Client.MVVM.View.Windows;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.Results;
using Client.MVVM.ViewModel.Observables;
using Client.MVVM.Model.BsonStorages;

namespace Client.MVVM.ViewModel.ServerActions
{
    public class EditServerViewModel : ServerEditorViewModel
    {
        public EditServerViewModel(Storage storage,
            LocalUserPrimaryKey loggedUserKey, ServerPrimaryKey serverKey)
            : base(storage)
        {
            var currentWindowLoadedHandler = WindowLoaded;
            WindowLoaded = new RelayCommand(e =>
            {
                currentWindowLoadedHandler.Execute(e);
                var win = (FormWindow)window;

                var server = _storage.GetServer(loggedUserKey, serverKey);
                win.AddTextField("|Name|", server.Name);
                win.AddTextField("|IP address|", serverKey.IpAddress.ToString());
                win.AddTextField("|Port|", serverKey.Port.ToString());
                win.AddTextField("|GUID|", server.Guid.ToString());

                var publicKey = server.PublicKey;
                win.AddTextField("|Public key|", publicKey != null ? publicKey.ToString() : "");
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls;

                _storage.GetServer(loggedUserKey, serverKey);

                // nie walidujemy, bo jest to przechowywane w polu w BSONie, a nie w SQLu
                var name = ((TextBox)fields[0]).Text;

                if (!ParseIpAddress(((TextBox)fields[1]).Text, out IPv4Address ipAddress))
                    return;

                if (!ParsePort(((TextBox)fields[2]).Text, out Port port))
                    return;

                if (!ParseGuid(((TextBox)fields[3]).Text, out Guid guid))
                    return;

                if (!ParsePublicKey(((TextBox)fields[4]).Text, out PublicKey publicKey))
                    return;

                // _storage.GetServer wyrzuci Error, jeżeli serwer nie istnieje.

                // jeżeli zmieniamy klucz główny, czyli (ip, port)
                var newServerKey = new ServerPrimaryKey(ipAddress, port);
                if (!newServerKey.Equals(serverKey)
                    && _storage.ServerExists(loggedUserKey, newServerKey))
                {
                    Alert(ServersStorage.AlreadyExistsMsg(newServerKey));
                    return;
                }

                var updatedServer = new Server(newServerKey)
                {
                    Name = name,
                    Guid = guid,
                    PublicKey = publicKey,
                };
                try { _storage.UpdateServer(loggedUserKey, serverKey, updatedServer); }
                catch (Error e)
                {
                    e.Prepend("|Could not| |update| |server;D| |in user's database.|");
                    Alert(e.Message);
                    throw;
                }
                OnRequestClose(new Success(updatedServer));
            });
        }

        private bool ParseGuid(string text, out Guid guid)
        {
            guid = Guid.Empty;
            if (string.IsNullOrEmpty(text))
                // uznajemy, że użytkownik nie chce podać GUIDu, więc zostawiamy Guid.Empty
                return true;
            try { guid = Guid.Parse(text); }
            catch (Exception e)
            {
                var error = new Error(e, "|Invalid GUID format.|");
                Alert(error.Message);
                return false;
            }
            return true;
        }

        private bool ParsePublicKey(string text, out PublicKey publicKey)
        {
            publicKey = null;
            if (string.IsNullOrEmpty(text))
                return true;
            try { publicKey = PublicKey.Parse(text); }
            catch (Error e)
            {
                e.Prepend("|Invalid public key format.|");
                Alert(e.Message);
                return false;
            }
            return true;
        }
    }
}
