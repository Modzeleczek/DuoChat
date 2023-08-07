using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.ViewModel.Results;
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
                win.AddTextField("|Login|");
                win.AddHoverableTextField("|Private key|",
                    new string[] { nameof(GeneratePrivateKey)},
                    new string[] { "|Generate private key|" });
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls;

                if (!ServerExists(loggedUser, server))
                {
                    var e = new Error($"|Server with IP address| {server.IpAddress} " +
                        $"|and port| {server.Port} |does not exist.|");
                    Alert(e.Message);
                    throw e;
                }

                var login = ((TextBox)fields[0]).Text;
                if (!ValidateLogin(login))
                    return;

                if (AccountExists(loggedUser, server, login))
                {
                    Alert(AccountExistsError(login));
                    return;
                }
                
                if (!ParsePrivateKey(((TextBox)fields[1]).Text, out PrivateKey privateKey))
                    return;

                var newAccount = new Account
                {
                    Login = login,
                    PrivateKey = privateKey
                };
                try { loggedUser.AddAccount(server.IpAddress, server.Port, newAccount); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |adding| " +
                        "|account;D| |to server's database.|");
                    Alert(e.Message);
                    throw;
                }
                OnRequestClose(new Success(newAccount));
            });
        }
    }
}
