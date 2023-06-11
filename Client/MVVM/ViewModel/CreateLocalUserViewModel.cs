using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
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

            Confirm = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                var userName = ((TextBox)fields[0]).Text;
                var unValSta = LocalUsersStorage.ValidateUserName(userName);
                if (unValSta.Code != 0)
                {
                    Alert(unValSta.Message);
                    return;
                }

                var password = ((PasswordBox)fields[1]).SecurePassword;
                var confirmedPassword = ((PasswordBox)fields[2]).SecurePassword;
                if (!pc.SecureStringsEqual(password, confirmedPassword))
                {
                    Alert("|Passwords do not match.|");
                    return;
                }
                var valSta = pc.ValidatePassword(password);
                if (valSta.Code != 0)
                {
                    Alert(valSta.Message);
                    return;
                }

                var existsStatus = lus.Exists(userName);
                if (existsStatus.Code < 0)
                {
                    existsStatus.Prepend("|Error occured while| |checking if| |user| " +
                        "|already exists.");
                    Alert(existsStatus.Message);
                    return;
                }
                if (existsStatus.Code == 0)
                {
                    Alert(existsStatus.Message);
                    return;
                }
                // użytkownik jeszcze nie istnieje
                var newUser = new LocalUser(userName, password);
                var addStatus = lus.Add(newUser);
                if (addStatus.Code != 0)
                {
                    addStatus.Prepend("|Error occured while| |adding| |user to database.|");
                    Alert(addStatus.Message);
                    return;
                }

                // zaszyfrowujemy katalog użytkownika jego hasłem
                var encryptStatus = ProgressBarViewModel.ShowDialog(window,
                    "|Encrypting user's database.|", false,
                    (reporter) =>
                    pc.EncryptDirectory(reporter,
                        newUser.DirectoryPath,
                        pc.ComputeDigest(password, newUser.DbSalt),
                        newUser.DbInitializationVector));
                if (encryptStatus.Code == 1)
                {
                    encryptStatus.Prepend("|You should not have canceled database encryption. " +
                        "It may have been corrupted.|");
                    Alert(encryptStatus.Message);
                    return;
                }
                else if (encryptStatus.Code != 0)
                {
                    encryptStatus.Prepend("|Error occured while| " +
                        "|encrypting user's database.| |Database may have been corrupted.|");
                    Alert(encryptStatus.Message);
                    return;
                }

                password.Dispose();
                confirmedPassword.Dispose();
                OnRequestClose(new Status(0, newUser));
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
