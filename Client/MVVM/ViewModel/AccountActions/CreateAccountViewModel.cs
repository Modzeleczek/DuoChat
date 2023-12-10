using Client.MVVM.Model;
using Client.MVVM.Model.SQLiteStorage.Repositories;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.AccountActions
{
    public class CreateAccountViewModel : AccountEditorViewModel
    {
        public CreateAccountViewModel(Storage storage,
            LocalUserPrimaryKey loggedUserKey, ServerPrimaryKey serverKey)
            : base(storage)
        {
            var currentWindowLoadedHandler = WindowLoaded!;
            WindowLoaded = new RelayCommand(e =>
            {
                currentWindowLoadedHandler.Execute(e);
                var win = (FormWindow)window!;
                win.AddTextField("|Login|");
                win.AddHoverableTextField("|Private key|",
                    new string[] { nameof(GeneratePrivateKey)},
                    new string[] { "|Generate private key|" });
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls!;

                // Wyrzuci Error, jeżeli serwer nie istnieje.
                _storage.GetServer(loggedUserKey, serverKey);

                var login = ((TextBox)fields[0]).Text;
                if (!ValidateLogin(login))
                    return;

                if (AccountExists(loggedUserKey, serverKey, login))
                {
                    Alert(AccountRepository.AlreadyExistsMsg(login));
                    return;
                }
                
                if (!ParsePrivateKey(((TextBox)fields[1]).Text, out PrivateKey? privateKey))
                    return;

                var newAccount = new Account
                {
                    Login = login,
                    PrivateKey = privateKey!
                };
                try { _storage.AddAccount(loggedUserKey, serverKey, newAccount); }
                catch (Error e)
                {
                    e.Prepend("|Could not| |add| " +
                        "|account;D| |to server's database.|");
                    Alert(e.Message);
                    throw;
                }
                OnRequestClose(new Success(newAccount));
            });
        }
    }
}
