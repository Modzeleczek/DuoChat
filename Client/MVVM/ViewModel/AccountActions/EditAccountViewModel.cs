using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.Model.SQLiteStorage.Repositories;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.AccountActions
{
    public class EditAccountViewModel : AccountEditorViewModel
    {
        public EditAccountViewModel(Storage storage,
            LocalUserPrimaryKey loggedUserKey, ServerPrimaryKey serverKey, string accountLogin)
            : base(storage)
        {
            var currentWindowLoadedHandler = WindowLoaded;
            WindowLoaded = new RelayCommand(e =>
            {
                currentWindowLoadedHandler.Execute(e);
                var win = (FormWindow)window;

                var account = _storage.GetAccount(loggedUserKey, serverKey, accountLogin);
                win.AddTextField("|Login|", account.Login);
                win.AddHoverableTextField("|Private key|",
                    new string[] { nameof(GeneratePrivateKey) },
                    new string[] { "|Generate private key|" },
                    account.PrivateKey.ToString());
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls;

                // Wyrzuci Error, jeżeli konto nie istnieje.
                var account = _storage.GetAccount(loggedUserKey, serverKey, accountLogin);

                var newLogin = ((TextBox)fields[0]).Text;
                if (!ValidateLogin(newLogin))
                    return;

                /* _storage.GetAccount wyrzuci Error, jeżeli konto
                nie istnieje, więc tu już nie sprawdzamy. */

                // jeżeli zmieniamy klucz główny, czyli (login)
                if (account.Login != newLogin)
                {
                    if (AccountExists(loggedUserKey, serverKey, newLogin))
                    {
                        Alert(AccountRepository.AlreadyExistsMsg(newLogin));
                        return;
                    }
                }

                if (!ParsePrivateKey(((TextBox)fields[1]).Text, out PrivateKey privateKey))
                    return;

                var updatedAccount = new Account
                {
                    Login = newLogin,
                    PrivateKey = privateKey
                };
                try
                {
                    _storage.UpdateAccount(loggedUserKey, serverKey, account.Login,
                        updatedAccount);
                }
                catch (Error e)
                {
                    e.Prepend("|Could not| |update| |account;D| " +
                        "|in server's database.|");
                    Alert(e.Message);
                    throw;
                }
                OnRequestClose(new Success(updatedAccount));
            });
        }
    }
}
