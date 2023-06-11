using Client.MVVM.Model;
using Shared.MVVM.Core;
using System.Windows.Controls;
using System;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;
using System.Collections.Generic;
using Client.MVVM.View.Windows;

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

            Confirm = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                // nie walidujemy, bo jest to przechowywane w polu w BSONie, a nie w SQLu
                var name = ((TextBox)fields[0]).Text;

                if (!ParseIpAddress(((TextBox)fields[1]).Text, out IPv4Address ipAddress))
                    return;

                if (!ParsePort(((TextBox)fields[2]).Text, out Port port))
                    return;

                var existsStatus = ServerExists(loggedUser, ipAddress, port);
                if (existsStatus.Code != 1)
                {
                    // błąd lub serwer istnieje
                    Alert(existsStatus.Message);
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
                var addStatus = loggedUser.AddServer(newServer);
                if (addStatus.Code != 0)
                {
                    addStatus.Prepend("|Error occured while| |adding| |server;D| " +
                        "|to user's database.|");
                    Alert(addStatus.Message);
                    return;
                }
                OnRequestClose(new Status(0, newServer));
            });
        }
    }
}
