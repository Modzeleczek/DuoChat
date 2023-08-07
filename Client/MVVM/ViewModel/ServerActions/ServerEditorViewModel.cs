using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
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
            try
            {
                ipAddress = IPv4Address.Parse(text);
                return true;
            }
            catch (Error e)
            {
                e.Prepend("|Invalid IP address format.|");
                Alert(e.Message);
                ipAddress = null;
                return false;
            }
        }

        protected bool ParsePort(string text, out Port port)
        {
            try
            {
                port = Port.Parse(text);
                return true;
            }
            catch (Error e)
            {
                e.Prepend("|Invalid port format.|");
                Alert(e.Message);
                port = null;
                return false;
            }
        }

        protected bool ServerExists(LocalUser user, IPv4Address ipAddress, Port port)
        {
            try { return user.ServerExists(ipAddress, port); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| |checking if| |server| " +
                    "|already exists.|");
                Alert(e.Message);
                throw;
            }
        }

        protected string ServerAlreadyExistsError(IPv4Address ipAddress, Port port) =>
            $"|Server with IP address| {ipAddress} |and port| {port} |already exists.|";
    }
}
