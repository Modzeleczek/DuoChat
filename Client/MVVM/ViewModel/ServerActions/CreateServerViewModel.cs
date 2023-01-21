using Client.MVVM.Model.BsonStorages;
using Client.MVVM.Model;
using Shared.MVVM.Core;
using System.Windows.Controls;
using System;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.View.Windows;

namespace Client.MVVM.ViewModel
{
    public class CreateServerViewModel : FormViewModel
    {
        public CreateServerViewModel(LocalUser loggedUser)
        {
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(e => window = (DialogWindow)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;

                var ipAddressStr = ((TextBox)inpCtrls[0]).Text;
                var ipParseStatus = IPv4Address.TryParse(ipAddressStr);
                if (ipParseStatus.Code != 0)
                {
                    Alert(d["Invalid IP address format."] + " " + ipParseStatus.Message);
                    return;
                }
                var ipAddress = (IPv4Address)ipParseStatus.Data;

                var portStr = ((TextBox)inpCtrls[1]).Text;
                var portParseStatus = Port.TryParse(portStr);
                if (portParseStatus.Code != 0)
                {
                    Alert(d["Invalid port format."] + " " + portParseStatus.Message);
                    return;
                }
                var port = (Port)portParseStatus.Data;

                if (!loggedUser.DirectoryExists())
                {
                    Alert(d["User's database does not exist."]);
                    return;
                }
                
                if (loggedUser.ServerExists(ipAddress, port))
                {
                    Alert(d["Server with IP address"] + $" {ipAddress} " +
                        "and port" + $" {port} " + d["already exists."]);
                    return;
                }
                var newServer = new Server
                {
                    Guid = Guid.Empty,
                    PublicKey = null,
                    IpAddress = ipAddress,
                    Port = port,
                    Name = null
                };
                var status = loggedUser.AddServer(newServer);
                if (status.Code != 0)
                {
                    Alert(status.Message);
                    return;
                }
                OnRequestClose(new Status(0, null, newServer));
            });
        }
    }
}
