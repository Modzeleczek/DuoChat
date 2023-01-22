using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.View.Windows;
using System.Security;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class ChangeLocalUserPasswordViewModel : PasswordFormViewModel
    {
        public ChangeLocalUserPasswordViewModel(LocalUser user, SecureString oldPassword)
        {
            var pc = new PasswordCryptography();
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(e => window = (DialogWindow)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var password = ((PasswordBox)inpCtrls[0]).SecurePassword;
                var confirmedPassword = ((PasswordBox)inpCtrls[1]).SecurePassword;
                if (!pc.SecureStringsEqual(password, confirmedPassword))
                {
                    Alert(d["Passwords do not match."]);
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
                    ensureStatus.Prepend(d["Error occured while"],
                        d["ensuring valid user database state."]);
                    Alert(ensureStatus.Message);
                    return;
                }

                // jeżeli katalog z plikami bazy danych istnieje, to odszyfrowujemy je starym hasłem
                var decryptStatus = ProgressBarViewModel.ShowDialog(window,
                    d["Decrypting user's database."], true,
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
                    decryptStatus.Prepend(d["Error occured while"],
                        d["decrypting user's database."]);
                    Alert(decryptStatus.Message);
                    return;
                }

                // wyznaczamy nową sól i skrót hasła oraz IV i sól bazy danych
                user.ResetPassword(password);

                // zaszyfrowujemy plik bazy danych nowym hasłem
                var encryptStatus = ProgressBarViewModel.ShowDialog(window,
                    d["Encrypting user's database."], false,
                    (reporter) =>
                    pc.EncryptDirectory(reporter,
                        user.DirectoryPath,
                        pc.ComputeDigest(password, user.DbSalt),
                        user.DbInitializationVector));
                if (encryptStatus.Code == 1)
                {
                    encryptStatus.Prepend(d["You should not have canceled database decryption. It may have been corrupted."]);
                    Alert(encryptStatus.Message);
                    return;
                }
                else if (encryptStatus.Code != 0)
                {
                    encryptStatus.Prepend(d["Error occured while"], d["encrypting user's database."],
                        d["Database may have been corrupted."]);
                    Alert(encryptStatus.Message);
                    return;
                }

                var updateStatus = lus.Update(user.Name, user);
                if (updateStatus.Code != 0)
                {
                    updateStatus.Prepend(d["Error occured while"], d["updating user in database."]);
                    Alert(updateStatus.Message);
                    return;
                }
                password.Dispose();
                confirmedPassword.Dispose();
                OnRequestClose(new Status(0));
            });
        }

        protected override void DisposePasswords(Control[] controls)
        {
            ((PasswordBox)controls[0]).SecurePassword.Dispose();
            ((PasswordBox)controls[1]).SecurePassword.Dispose();
        }
    }
}
