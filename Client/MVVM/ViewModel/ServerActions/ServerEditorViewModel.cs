using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Networking;

namespace Client.MVVM.ViewModel.ServerActions
{
    public class ServerEditorViewModel : FormViewModel
    {
        protected ServerEditorViewModel()
        {
            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                RequestClose += () => win.Close();
            });
        }

        protected bool ParseIpAddress(string text, out IPv4Address ipAddress)
        {
            ipAddress = null;
            var status = IPv4Address.TryParse(text);
            if (status.Code != 0)
            {
                status.Prepend(d["Invalid IP address format."]);
                Alert(status.Message);
                return false;
            }
            ipAddress = (IPv4Address)status.Data;
            return true;
        }

        protected bool ParsePort(string text, out Port port)
        {
            port = null;
            var status = Port.TryParse(text);
            if (status.Code != 0)
            {
                status.Prepend(d["Invalid port format."]);
                Alert(status.Message);
                return false;
            }
            port = (Port)status.Data;
            return true;
        }

        protected Status ServerExists(LocalUser user, IPv4Address ipAddress, Port port)
        {
            var status = user.ServerExists(ipAddress, port);
            if (status.Code < 0)
                status.Prepend(d["Error occured while"],
                    d["checking if"], d["server"], d["already exists."]);
            return status;
        }
    }
}
