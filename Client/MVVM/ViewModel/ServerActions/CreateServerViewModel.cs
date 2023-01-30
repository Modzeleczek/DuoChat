using Client.MVVM.Model.BsonStorages;
using Client.MVVM.Model;
using Shared.MVVM.Core;
using System.Windows.Controls;
using System;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;
using System.Collections.Generic;
using Client.MVVM.View.Windows;

namespace Client.MVVM.ViewModel
{
    public class CreateServerViewModel : FormViewModel
    {
        public CreateServerViewModel(LocalUser loggedUser)
        {
            var lus = new LocalUsersStorage();

            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                win.AddTextField(d["IP address"]);
                win.AddTextField(d["Port"]);
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                var ipAddressStr = ((TextBox)fields[0]).Text;
                var ipParseStatus = IPv4Address.TryParse(ipAddressStr);
                if (ipParseStatus.Code != 0)
                {
                    ipParseStatus.Prepend(d["Invalid IP address format."]);
                    Alert(ipParseStatus.Message);
                    return;
                }
                var ipAddress = (IPv4Address)ipParseStatus.Data;

                var portStr = ((TextBox)fields[1]).Text;
                var portParseStatus = Port.TryParse(portStr);
                if (portParseStatus.Code != 0)
                {
                    portParseStatus.Prepend(d["Invalid port format."]);
                    Alert(portParseStatus.Message);
                    return;
                }
                var port = (Port)portParseStatus.Data;

                var existsStatus = loggedUser.ServerExists(ipAddress, port);
                if (existsStatus.Code < 0)
                {
                    existsStatus.Prepend(d["Error occured while"],
                        d["checking if"], d["server"], d["already exists."]);
                    Alert(existsStatus.Message);
                    return;
                }
                if (existsStatus.Code == 0)
                {
                    Alert(existsStatus.Message);
                    return;
                }
                // existsStatus.Code == 1, czyli serwer jeszcze nie istnieje
                var newServer = new Server
                {
                    Guid = Guid.Empty,
                    PublicKey = null,
                    IpAddress = ipAddress,
                    Port = port,
                    Name = null
                };
                var addStatus = loggedUser.AddServer(newServer);
                if (addStatus.Code != 0)
                {
                    addStatus.Prepend(d["Error occured while"], d["adding"],
                        d["server to user's database."]);
                    Alert(addStatus.Message);
                    return;
                }
                OnRequestClose(new Status(0, newServer));
            });
        }
    }
}
