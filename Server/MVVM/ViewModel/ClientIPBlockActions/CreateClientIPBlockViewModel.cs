using Shared.MVVM.Core;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Server.MVVM.ViewModel.ClientIPBlockActions
{
    public class CreateClientIPBlockViewModel : FormViewModel
    {
        public CreateClientIPBlockViewModel()
        {
            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e!;
                window = win;
                RequestClose += () => win.Close();

                win.AddTextField("|IP address|");
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls!;

                if (!ParseIpAddress(((TextBox)fields[0]).Text, out IPv4Address? ipAddress))
                    return;

                OnRequestClose(new Success(ipAddress));
            });
        }

        private bool ParseIpAddress(string text, out IPv4Address? ipAddress)
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
    }
}
