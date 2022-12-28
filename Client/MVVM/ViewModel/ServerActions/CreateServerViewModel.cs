using Client.MVVM.Model.BsonStorages;
using Client.MVVM.Model;
using Shared.MVVM.Core;
using System.Windows;
using System.Windows.Controls;
using System.Net;
using System;
using Client.MVVM.Model.XamlObservables;

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
                    Error(d["Invalid IP address format."]);
                    return;
                }
                var portStr = ((TextBox)inpCtrls[1]).Text;
                if (!ushort.TryParse(portStr, out ushort port))
                {
                    Error(d["Invalid port format."]);
                    return;
                }
                var db = loggedUser.GetDatabase();
                if (!db.Exists())
                {
                    Error(d["User's database does not exist. An empty database will be created."]);
                    db.Create();
                }
                var serSto = db.GetServersStorage();
                // var serGuid = Guid.Parse("CE213984-6F1D-4845-9CBF-58A3675DDCF9");
                var serGuid = Guid.NewGuid();
                if (serSto.Exists(serGuid))
                {
                    Error(d["Server with GUID"] + $" {serGuid} " + d["already exists."]);
                    return;
                }
                var newServer = new Server
                {
                    GUID = serGuid,
                    IpAddress = ipAddress,
                    Port = port,
                    Name = "przykładowa nazwa"
                };
                var status = serSto.Add(newServer.ToSerializable());
                if (status.Code != 0)
                {
                    Error(status.Message);
                    return;
                }
                OnRequestClose(new Status(0, null, newServer));
            });
        }
    }
}
