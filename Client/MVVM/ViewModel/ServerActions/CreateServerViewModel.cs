using Shared.MVVM.Core;
using System.Windows.Controls;
using System;
using Shared.MVVM.Model.Networking;
using System.Collections.Generic;
using Client.MVVM.View.Windows;
using Shared.MVVM.ViewModel.Results;
using Client.MVVM.ViewModel.Observables;

namespace Client.MVVM.ViewModel.ServerActions
{
    public class CreateServerViewModel : ServerEditorViewModel
    {
        public CreateServerViewModel(LocalUser loggedUser)
        {
            var currentWindowLoadedHandler = WindowLoaded;
            WindowLoaded = new RelayCommand(e =>
            {
                currentWindowLoadedHandler.Execute(e);
                var win = (FormWindow)window;
                win.AddTextField("|Name|");
                win.AddTextField("|IP address|");
                win.AddTextField("|Port|");
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

                if (ServerExists(loggedUser, ipAddress, port))
                {
                    Alert(ServerAlreadyExistsError(ipAddress, port));
                    return;
                }

                var newServer = new Server
                {
                    Name = name,
                    IpAddress = ipAddress,
                    Port = port,
                    Guid = Guid.Empty,
                    PublicKey = null,
                };
                try { loggedUser.AddServer(newServer); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |adding| |server;D| " +
                        "|to user's database.|");
                    Alert(e.Message);
                    throw;
                }
                OnRequestClose(new Success(newServer));
            });
        }
    }
}
