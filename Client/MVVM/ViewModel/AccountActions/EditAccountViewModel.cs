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
    public class EditAccountViewModel : AccountEditorViewModel
    {
        public EditAccountViewModel(LocalUser loggedUser, Server server, Account account)
        {
            var lus = new LocalUsersStorage();

            var currentWindowLoadedHandler = WindowLoaded;
            WindowLoaded = new RelayCommand(e =>
            {
                currentWindowLoadedHandler.Execute(e);
                var win = (FormWindow)window;
                win.AddTextField(d["Login"], account.Login);
                win.AddHoverableTextField(d["Private key"],
                    new string[] { nameof(GeneratePrivateKey) },
                    new string[] { d["Generate private key"] },
                    account.PrivateKey.ToString());
            });

            Confirm = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                if (!ServerExists(loggedUser, server))
                    // błąd lub serwer nie istnieje
                    return;

                var login = ((TextBox)fields[0]).Text;
                if (!ValidateLogin(login))
                    return;

                var oldExStatus = AccountExists(loggedUser, server, account.Login);
                if (oldExStatus.Code != 0)
                {
                    // błąd lub konto nie istnieje
                    Alert(oldExStatus.Message);
                    return;
                }

                // jeżeli zmieniamy klucz główny, czyli (login)
                if (account.Login != login)
                {
                    var newExStatus = AccountExists(loggedUser, server, login);
                    if (newExStatus.Code != 1)
                    {
                        // błąd lub konto istnieje
                        Alert(newExStatus.Message);
                        return;
                    }
                }

                if (!ParsePrivateKey(((TextBox)fields[1]).Text, out PrivateKey privateKey))
                    return;

                var updatedAccount = new Account
                {
                    Login = login,
                    PrivateKey = privateKey
                };
                var updateStatus = loggedUser.UpdateAccount(server.IpAddress, server.Port,
                    account.Login, updatedAccount);
                if (updateStatus.Code != 0)
                {
                    updateStatus.Prepend(d["Error occured while"], d["updating"],
                        d["account;D"], d["in server's database."]);
                    Alert(updateStatus.Message);
                    return;
                }
                updatedAccount.CopyTo(account);
                OnRequestClose(new Status(0));
            });
        }
    }
}
