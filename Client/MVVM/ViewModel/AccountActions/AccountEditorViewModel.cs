using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Cryptography;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.AccountActions
{
    public class AccountEditorViewModel : FormViewModel
    {
        #region Properties
        public RelayCommand GeneratePrivateKey { get; private set; }
        #endregion

        protected AccountEditorViewModel()
        {
            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                RequestClose += () => win.Close();
            });

            GeneratePrivateKey = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                var status = ProgressBarViewModel.ShowDialog(window,
                    "|Private key generation|", true,
                    (reporter) => PrivateKey.Random(reporter));
                if (status.Code == 1) return; // anulowano

                var textBox = (TextBox)fields[1];
                textBox.Text = ((PrivateKey)status.Data).ToString();
            });
        }

        protected bool ServerExists(LocalUser user, Server server)
        {
            var status = user.ServerExists(server.IpAddress, server.Port);
            if (status.Code < 0)
            {
                status.Prepend("|Error occured while| |checking if| |server| |already exists.|");
                Alert(status.Message);
                return false;
            }
            else if (status.Code == 1)
            {
                Alert(status.Message);
                return false;
            }
            // status.Code == 0, czyli serwer istnieje
            return true;
        }

        protected bool ValidateLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                Alert("|Login cannot be empty.|");
                return false;
            }
            // aby zapobiec SQLInjection, dopuszczamy tylko duże i małe litery oraz cyfry
            foreach (var c in login)
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                {
                    Alert("|Login may contain only letters and digits.|");
                    return false;
                }
            return true;
        }

        protected Status AccountExists(LocalUser user, Server server, string login)
        {
            var status = user.AccountExists(server.IpAddress, server.Port, login);
            if (status.Code < 0)
                status.Prepend("|Error occured while| |checking if| |account| " +
                    "|already exists.|");
            return status;
        }

        protected bool ParsePrivateKey(string text, out PrivateKey privateKey)
        {
            privateKey = null;
            // klucz prywatny walidujemy jako ostatni, bo najdłużej to trwa
            var status = ProgressBarViewModel.ShowDialog(window,
                "|Private key validation|", true,
                (reporter) => PrivateKey.TryParse(reporter, text));
            if (status.Code == 1)
                return false; // anulowano
            // błędy zostały już wyświetlone w ProgressBarViewModelu
            if (status.Code < 0)
                return false;
            // status.Code == 0
            privateKey = (PrivateKey)status.Data;
            return true;
        }
    }
}
