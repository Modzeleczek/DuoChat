using Client.MVVM.Model;
using Shared.MVVM.Core;
using System.Windows.Controls;
using System;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;
using System.Collections.Generic;
using Client.MVVM.View.Windows;
using Shared.MVVM.Model.Cryptography;

namespace Client.MVVM.ViewModel.ServerActions
{
    public class EditServerViewModel : ServerEditorViewModel
    {
        public EditServerViewModel(LocalUser loggedUser, Server server)
        {
            var currentWindowLoadedHandler = WindowLoaded;
            WindowLoaded = new RelayCommand(e =>
            {
                currentWindowLoadedHandler.Execute(e);
                var win = (FormWindow)window;
                win.AddTextField("|Name|", server.Name);
                win.AddTextField("|IP address|", server.IpAddress.ToString());
                win.AddTextField("|Port|", server.Port.ToString());
                win.AddTextField("|GUID|", server.Guid.ToString());

                var publicKey = server.PublicKey;
                win.AddTextField("|Public key|", publicKey != null ? publicKey.ToString() : "");
            });

            Confirm = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

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

                var oldExStatus = ServerExists(loggedUser, server.IpAddress, server.Port);
                if (oldExStatus.Code != 0)
                {
                    // błąd lub serwer nie istnieje
                    Alert(oldExStatus.Message);
                    return;
                }

                // jeżeli zmieniamy klucz główny, czyli (ip, port)
                if (!server.KeyEquals(ipAddress, port))
                {
                    var newExStatus = ServerExists(loggedUser, ipAddress, port);
                    if (newExStatus.Code != 1)
                    {
                        // błąd lub serwer istnieje
                        Alert(newExStatus.Message);
                        return;
                    }
                }

                var updatedServer = new Server
                {
                    Name = name,
                    IpAddress = ipAddress,
                    Port = port,
                    Guid = guid,
                    PublicKey = publicKey,
                };
                var updateStatus = loggedUser.UpdateServer(server.IpAddress, server.Port,
                    updatedServer);
                if (updateStatus.Code != 0)
                {
                    updateStatus.Prepend("|Error occured while| |updating| |server;D| " +
                        "|in user's database.|");
                    Alert(updateStatus.Message);
                    return;
                }
                updatedServer.CopyTo(server);
                OnRequestClose(new Status(0));
            });
        }

        private bool ParseGuid(string text, out Guid guid)
        {
            guid = Guid.Empty;
            if (string.IsNullOrEmpty(text))
                // uznajemy, że użytkownik nie chce podać GUIDu, więc zostawiamy Guid.Empty
                return true;
            if (!Guid.TryParse(text, out Guid value))
            {
                Alert("|Invalid GUID format.|");
                return false;
            }
            guid = value;
            return true;
        }

        private bool ParsePublicKey(string text, out PublicKey publicKey)
        {
            publicKey = null;
            if (string.IsNullOrEmpty(text))
                return true;
            var status = PublicKey.TryParse(text);
            if (status.Code < 0)
            {
                status.Prepend("|Invalid public key format.|");
                Alert(status.Message);
                return false;
            }
            publicKey = (PublicKey)status.Data;
            return true;
        }
    }
}
