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
    public class CreateAccountViewModel : AccountEditorViewModel
    {
        public CreateAccountViewModel(LocalUser loggedUser, Server server)
        {
            var lus = new LocalUsersStorage();

            var currentWindowLoadedHandler = WindowLoaded;
            WindowLoaded = new RelayCommand(e =>
            {
                currentWindowLoadedHandler.Execute(e);
                var win = (FormWindow)window;
                win.AddTextField(d["Login"]);
                win.AddHoverableTextField(d["Private key"],
                    new string[] { nameof(GeneratePrivateKey)},
                    new string[] { d["Generate private key"] });
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

                var existsStatus = AccountExists(loggedUser, server, login);
                if (existsStatus.Code != 1)
                {
                    // błąd lub konto istnieje
                    Alert(existsStatus.Message);
                    return;
                }

                if (!ParsePrivateKey(((TextBox)fields[1]).Text, out PrivateKey privateKey))
                    return;

                var newAccount = new Account
                {
                    Login = login,
                    PrivateKey = privateKey
                };
                var addStatus = loggedUser.AddAccount(server.IpAddress, server.Port, newAccount);
                if (addStatus.Code != 0)
                {
                    addStatus.Prepend(d["Error occured while"], d["adding"],
                        d["account;D"], d["to server's database."]);
                    Alert(addStatus.Message);
                    return;
                }
                OnRequestClose(new Status(0, newAccount));
            });
        }
    }
}
