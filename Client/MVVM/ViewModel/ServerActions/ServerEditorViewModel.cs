using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.ViewModel;

namespace Client.MVVM.ViewModel.ServerActions
{
    public class ServerEditorViewModel : FormViewModel
    {
        #region Fields
        protected Storage _storage;
        #endregion

        protected ServerEditorViewModel(Storage storage)
        {
            _storage = storage;

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
    }
}
