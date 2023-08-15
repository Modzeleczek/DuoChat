using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Security;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.LocalUsers.LocalUserActions
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

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls;

                var password = ((PasswordBox)fields[0]).SecurePassword;
                var confirmedPassword = ((PasswordBox)fields[1]).SecurePassword;
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

                try { lus.EnsureValidDatabaseState(); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| " +
                        "|ensuring valid user database state.|");
                    Alert(e.Message);
                    throw;
                }

                // jeżeli katalog z plikami bazy danych istnieje, to odszyfrowujemy go starym hasłem
                var decryptRes = ProgressBarViewModel.ShowDialog(window,
                    "|Decrypting user's database.|", true,
                    (reporter) =>
                    pc.DecryptDirectory(reporter,
                        user.DirectoryPath,
                        pc.ComputeDigest(oldPassword, user.DbSalt),
                        user.DbInitializationVector));
                if (decryptRes is Cancellation)
                    return;
                else if (decryptRes is Failure failure)
                {
                    var e = failure.Reason;
                    // nie udało się odszyfrować katalogu użytkownika, więc crashujemy program
                    e.Prepend("|Error occured while| |decrypting user's database.|");
                    Alert(e.Message);
                    throw e;
                }

                // wyznaczamy nową sól i skrót hasła oraz IV i sól bazy danych
                user.ResetPassword(password);

                // zaszyfrowujemy katalog użytkownika nowym hasłem
                var encryptRes = ProgressBarViewModel.ShowDialog(window,
                    "|Encrypting user's database.|", false,
                    (reporter) =>
                    pc.EncryptDirectory(reporter,
                        user.DirectoryPath,
                        pc.ComputeDigest(password, user.DbSalt),
                        user.DbInitializationVector));
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

                try { lus.Update(user.Name, user); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |updating| |user in database.|");
                    Alert(e.Message);
                    throw;
                }
                password.Dispose();
                confirmedPassword.Dispose();
                OnRequestClose(new Success());
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
