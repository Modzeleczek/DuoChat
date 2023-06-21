using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.Results;
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

                var result = ProgressBarViewModel.ShowDialog(window,
                    "|Private key generation|", true,
                    (reporter) => PrivateKey.Random(reporter));
                if (!(result is Success success)) return; // anulowano

                var textBox = (TextBox)fields[1];
                textBox.Text = ((PrivateKey)success.Data).ToString();
            });
        }

        protected bool ServerExists(LocalUser user, Server server)
        {
            try { return user.ServerExists(server.IpAddress, server.Port); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| |checking if| |server| |already exists.|");
                Alert(e.Message);
                throw;
            }
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

        protected bool AccountExists(LocalUser user, Server server, string login)
        {
            try { return user.AccountExists(server.IpAddress, server.Port, login); }
            catch (Error e)
            {
                e.Prepend("|Error occured while| |checking if| |account| " +
                    "|already exists.|");
                Alert(e.Message);
                throw;
            }
        }

        protected string AccountExistsError(string login) =>
            $"|Account with login| {login} |already exists.|";

        protected bool ParsePrivateKey(string text, out PrivateKey privateKey)
        {
            privateKey = null;
            // klucz prywatny walidujemy jako ostatni, bo najdłużej to trwa
            var result = ProgressBarViewModel.ShowDialog(window,
                "|Private key validation|", true,
                (reporter) => PrivateKey.Parse(reporter, text));

            // anulowano lub błąd (błędy zostały już wyświetlone w ProgressBarViewModelu)
            if (!(result is Success success))
                return false;

            // powodzenie
            privateKey = (PrivateKey)success.Data;
            return true;
        }
    }
}
