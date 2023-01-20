using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class ChangeLocalUserPasswordViewModel : PasswordFormViewModel
    {
        public ChangeLocalUserPasswordViewModel(LocalUser user, SecureString oldPassword)
        {
            var pc = new PasswordCryptography();
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(e => window = (Window)e);
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
                if (!user.DirectoryExists())
                {
                    Alert(d["User's database does not exist."]);
                    return;
                }
                // jeżeli katalog z plikami bazy danych istnieje, to odszyfrowujemy je starym hasłem
                var decSta = ProgressBarViewModel.ShowDialog(window,
                    d["Decrypting user's database."], true,
                    (reporter) =>
                    pc.DecryptDirectory(reporter,
                        user.DirectoryPath,
                        pc.ComputeDigest(oldPassword, user.DbSalt),
                        user.DbInitializationVector));
                if (decSta.Code == 1)
                {
                    Alert(d["Password change canceled."]);
                    return;
                }
                else if (decSta.Code != 0) return;
                // wyznaczamy nową sól i skrót hasła oraz IV i sól bazy danych
                user.ResetPassword(password);
                // zaszyfrowujemy plik bazy danych nowym hasłem
                var encSta = ProgressBarViewModel.ShowDialog(window,
                    d["Encrypting user's database."], false,
                    (reporter) =>
                    pc.EncryptDirectory(reporter,
                        user.DirectoryPath,
                        pc.ComputeDigest(password, user.DbSalt),
                        user.DbInitializationVector));
                if (encSta.Code == 1)
                {
                    Alert(d["You should not have canceled database decryption. It may have been corrupted."]);
                    return;
                }
                if (encSta.Code != 0)
                {
                    Alert(d["Database may have been corrupted."]);
                    return;
                }

                var updSta = lus.Update(user.Name, user.ToSerializable());
                if (updSta.Code != 0)
                {
                    Alert(updSta.Message);
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
