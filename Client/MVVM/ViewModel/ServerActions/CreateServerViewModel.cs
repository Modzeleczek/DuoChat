using Client.MVVM.Model.BsonStorages;
using Client.MVVM.Model;
using Shared.MVVM.Core;
using System.Windows;
using System.Windows.Controls;
using System;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Cryptography;

namespace Client.MVVM.ViewModel
{
    public class CreateServerViewModel : FormViewModel
    {
        public CreateServerViewModel(LocalUser loggedUser)
        {
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var ipAddressStr = ((TextBox)inpCtrls[0]).Text;
                if (!IPv4Address.TryParse(ipAddressStr, out IPv4Address ipAddress))
                {
                    Alert(d["Invalid IP address format."]);
                    return;
                }
                var portStr = ((TextBox)inpCtrls[1]).Text;
                if (!ushort.TryParse(portStr, out ushort port))
                {
                    Alert(d["Invalid port format."]);
                    return;
                }
                if (!loggedUser.DirectoryExists())
                {
                    Alert(d["User's database does not exist."]);
                    return;
                }
                var serGuid = Guid.NewGuid();
                if (loggedUser.ServerExists(serGuid))
                {
                    Alert(d["Server with GUID"] + $" {serGuid} " + d["already exists."]);
                    return;
                }
                var newServer = new Server
                {
                    Guid = serGuid,
                    PublicKey = new PublicKey(new byte[] {0b0000_1111}),
                    IpAddress = ipAddress,
                    Port = port,
                    Name = "przykładowa nazwa"
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
