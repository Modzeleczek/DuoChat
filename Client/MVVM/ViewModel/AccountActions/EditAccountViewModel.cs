using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.Results;
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
                win.AddTextField("|Login|", account.Login);
                win.AddHoverableTextField("|Private key|",
                    new string[] { nameof(GeneratePrivateKey) },
                    new string[] { "|Generate private key|" },
                    account.PrivateKey.ToString());
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls;

                if (!ServerExists(loggedUser, server))
                    // błąd lub serwer nie istnieje
                    return;

                var login = ((TextBox)fields[0]).Text;
                if (!ValidateLogin(login))
                    return;

                if (!AccountExists(loggedUser, server, account.Login))
                {
                    Alert($"|Account with login| {login} |does not exist.|");
                    return;
                }

                // jeżeli zmieniamy klucz główny, czyli (login)
                if (account.Login != login)
                {
                    if (AccountExists(loggedUser, server, login))
                    {
                        Alert(AccountExistsError(login));
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
                try { loggedUser.UpdateAccount(server.IpAddress, server.Port,
                    account.Login, updatedAccount); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |updating| |account;D| " +
                        "|in server's database.|");
                    Alert(e.Message);
                    throw;
                }
                updatedAccount.CopyTo(account);
                OnRequestClose(new Success());
            });
        }
    }
}
