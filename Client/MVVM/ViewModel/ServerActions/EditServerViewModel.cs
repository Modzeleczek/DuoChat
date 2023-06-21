using Client.MVVM.Model;
using Shared.MVVM.Core;
using System.Windows.Controls;
using System;
using Shared.MVVM.Model.Networking;
using System.Collections.Generic;
using Client.MVVM.View.Windows;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.Results;

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

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls;

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

                if (!ServerExists(loggedUser, server.IpAddress, server.Port))
                {
                    Alert($"|Server with IP address| {ipAddress} " +
                        $"|and port| {port} |does not exist.|");
                    return;
                }

                // jeżeli zmieniamy klucz główny, czyli (ip, port)
                if (!server.KeyEquals(ipAddress, port))
                {
                    if (ServerExists(loggedUser, ipAddress, port))
                    {
                        Alert(ServerAlreadyExistsError(ipAddress, port));
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
                try { loggedUser.UpdateServer(server.IpAddress, server.Port,
                    updatedServer); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |updating| |server;D| " +
                        "|in user's database.|");
                    Alert(e.Message);
                    throw;
                }
                updatedServer.CopyTo(server);
                OnRequestClose(new Success());
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
