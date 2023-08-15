using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.LocalUsers.LocalUserActions
{
    public class CreateLocalUserViewModel : FormViewModel
    {
        public CreateLocalUserViewModel()
        {
            var pc = new PasswordCryptography();
            var lus = new LocalUsersStorage();

            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                win.AddTextField("|Username|");
                win.AddPasswordField("|Password|");
                win.AddPasswordField("|Confirm password|");
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls;

                var userName = ((TextBox)fields[0]).Text;
                var userNameVal = LocalUsersStorage.ValidateUserName(userName);
                if (!(userNameVal is null))
                {
                    Alert(userNameVal);
                    return;
                }

                var password = ((PasswordBox)fields[1]).SecurePassword;
                var confirmedPassword = ((PasswordBox)fields[2]).SecurePassword;
                if (!pc.SecureStringsEqual(password, confirmedPassword))
                {
                    Alert("|Passwords do not match.|");
                    return;
                }

                var passwordVal = pc.ValidatePassword(password);
                if (!(passwordVal is null))
                {
                    Alert(passwordVal);
                    return;
                }

                try
                {
                    if (lus.Exists(userName))
                    {
                        Alert($"|User with name| {userName} |already exists.|");
                        return;
                    }
                }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |checking if| |user| " +
                        "|already exists.|");
                    Alert(e.Message);
                    throw;
                }

                // użytkownik jeszcze nie istnieje
                var newUser = new LocalUser(userName, password);
                try { lus.Add(newUser); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |adding| |user to database.|");
                    Alert(e.Message);
                    throw;
                }

                // zaszyfrowujemy katalog użytkownika jego hasłem
                var encryptRes = ProgressBarViewModel.ShowDialog(window,
                    "|Encrypting user's database.|", false,
                    (reporter) =>
                    pc.EncryptDirectory(reporter,
                        newUser.DirectoryPath,
                        pc.ComputeDigest(password, newUser.DbSalt),
                        newUser.DbInitializationVector));
                if (encryptRes is Cancellation)
                {
                    var e = new Error("|You should not have canceled database encryption. " +
                        "It may have been corrupted.|");
                    Alert(e.Message);
                    throw e;
                }
                else if (encryptRes is Failure failure)
                {
                    var e = failure.Reason;
                    e.Prepend("|Error occured while| " +
                        "|encrypting user's database.| |Database may have been corrupted.|");
                    Alert(e.Message);
                    throw e;
                }

                password.Dispose();
                confirmedPassword.Dispose();
                OnRequestClose(new Success(newUser));
            });

            var defaultCloseHandler = Close;
            Close = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;
                ((PasswordBox)fields[1]).SecurePassword.Dispose();
                ((PasswordBox)fields[2]).SecurePassword.Dispose();
                // odpowiednik base.Close w nadpisanej metodzie wirtualnej
                defaultCloseHandler?.Execute(e);
            });
        }
    }
}
