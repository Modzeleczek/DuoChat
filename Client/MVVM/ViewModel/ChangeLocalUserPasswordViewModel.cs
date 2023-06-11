using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using System.Collections.Generic;
using System.Security;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class ChangeLocalUserPasswordViewModel : FormViewModel
    {
        public ChangeLocalUserPasswordViewModel(LocalUser user, SecureString oldPassword)
        {
            var pc = new PasswordCryptography();
            var lus = new LocalUsersStorage();

            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                win.AddPasswordField("|New password|");
                win.AddPasswordField("|Confirm password|");
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                var password = ((PasswordBox)fields[0]).SecurePassword;
                var confirmedPassword = ((PasswordBox)fields[1]).SecurePassword;
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

                var ensureStatus = lus.EnsureValidDatabaseState();
                if (ensureStatus.Code != 0)
                {
                    ensureStatus.Prepend("|Error occured while| " +
                        "|ensuring valid user database state.|");
                    Alert(ensureStatus.Message);
                    return;
                }

                // jeżeli katalog z plikami bazy danych istnieje, to odszyfrowujemy je starym hasłem
                var decryptStatus = ProgressBarViewModel.ShowDialog(window,
                    "|Decrypting user's database.|", true,
                    (reporter) =>
                    pc.DecryptDirectory(reporter,
                        user.DirectoryPath,
                        pc.ComputeDigest(oldPassword, user.DbSalt),
                        user.DbInitializationVector));
                if (decryptStatus.Code == 1)
                    return;
                // nie udało się odszyfrować katalogu użytkownika, więc anulujemy zmianę hasła
                else if (decryptStatus.Code != 0)
                {
                    decryptStatus.Prepend("|Error occured while| |decrypting user's database.|");
                    Alert(decryptStatus.Message);
                    return;
                }

                // wyznaczamy nową sól i skrót hasła oraz IV i sól bazy danych
                user.ResetPassword(password);

                // zaszyfrowujemy katalog użytkownika nowym hasłem
                var encryptStatus = ProgressBarViewModel.ShowDialog(window,
                    "|Encrypting user's database.|", false,
                    (reporter) =>
                    pc.EncryptDirectory(reporter,
                        user.DirectoryPath,
                        pc.ComputeDigest(password, user.DbSalt),
                        user.DbInitializationVector));
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

                var updateStatus = lus.Update(user.Name, user);
                if (updateStatus.Code != 0)
                {
                    updateStatus.Prepend("|Error occured while| |updating| |user in database.|");
                    Alert(updateStatus.Message);
                    return;
                }
                password.Dispose();
                confirmedPassword.Dispose();
                OnRequestClose(new Status(0));
            });

            var defaultCloseHandler = Close;
            Close = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;
                ((PasswordBox)fields[0]).SecurePassword.Dispose();
                ((PasswordBox)fields[1]).SecurePassword.Dispose();
                defaultCloseHandler?.Execute(e);
            });
        }
    }
}
