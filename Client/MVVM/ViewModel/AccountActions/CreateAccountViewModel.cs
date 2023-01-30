using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.Model.Cryptography;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.AccountActions
{
    public class CreateAccountViewModel : FormViewModel
    {
        #region Properties
        public RelayCommand GeneratePrivateKey { get; private set; }
        #endregion

        public CreateAccountViewModel(LocalUser loggedUser, Server server)
        {
            var lus = new LocalUsersStorage();

            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                win.AddTextField(d["Login"]);
                win.AddHoverableTextField(d["Private key"],
                    new string[] { nameof(GeneratePrivateKey)},
                    new string[] { d["Generate private key"] });
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                var login = ((TextBox)fields[0]).Text;
                if (!ValidateLogin(login))
                    return;

                var servExStatus = loggedUser.ServerExists(server.IpAddress, server.Port);
                if (servExStatus.Code < 0)
                {
                    servExStatus.Prepend(d["Error occured while"],
                        d["checking if"], d["server"], d["already exists."]);
                    Alert(servExStatus.Message);
                    return;
                }
                if (servExStatus.Code == 1)
                {
                    Alert(servExStatus.Message);
                    return;
                }
                // servExStatus.Code == 0, czyli serwer istnieje

                var accExStatus = loggedUser.AccountExists(server.IpAddress, server.Port, login);
                if (accExStatus.Code < 0)
                {
                    accExStatus.Prepend(d["Error occured while"],
                        d["checking if"], d["account"], d["already exists."]);
                    Alert(accExStatus.Message);
                    return;
                }
                if (accExStatus.Code == 0) // konto już istnieje
                {
                    Alert(accExStatus.Message);
                    return;
                }
                // accExStatus.Code == 1, czyli konto nie istnieje

                // klucz prywatny walidujemy jako ostatni, bo najdłużej to trwa
                var privateKeyStr = ((TextBox)fields[1]).Text;
                var keyParseStatus = ProgressBarViewModel.ShowDialog(window,
                    d["Private key validation"], true,
                    (reporter) => PrivateKey.TryParse(reporter, privateKeyStr));
                if (keyParseStatus.Code == 1)
                    return; // anulowano
                // błędy zostały już wyświetlone w ProgressBarViewModelu
                if (keyParseStatus.Code < 0)
                    return;
                // keyParseStatus.Code == 0
                var privateKey = (PrivateKey)keyParseStatus.Data;

                var newAccount = new Account
                {
                    Login = login,
                    PrivateKey = privateKey
                };
                var addStatus = loggedUser.AddAccount(server.IpAddress, server.Port, newAccount);
                if (addStatus.Code != 0)
                {
                    addStatus.Prepend(d["Error occured while"], d["adding"],
                        d["account to server's database."]);
                    Alert(addStatus.Message);
                    return;
                }
                OnRequestClose(new Status(0, newAccount));
            });

            GeneratePrivateKey = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                var genStatus = ProgressBarViewModel.ShowDialog(window,
                    d["Private key generation"], true,
                    (reporter) => PrivateKey.Random(reporter));
                if (genStatus.Code == 1) return; // anulowano

                var textBox = (TextBox)fields[1];
                textBox.Text = ((PrivateKey)genStatus.Data).ToString();
            });
        }

        private bool ValidateLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                Alert(d["Login cannot be empty."]);
                return false;
            }
            // aby zapobiec SQLInjection, dopuszczamy tylko duże i małe litery oraz cyfry
            foreach (var c in login)
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                {
                    Alert(d["Login may contain only letters and digits."]);
                    return false;
                }
            return true;
        }
    }
}
