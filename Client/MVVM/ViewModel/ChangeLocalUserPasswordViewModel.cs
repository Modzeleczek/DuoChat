using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Shared.MVVM.Core;
using System.ComponentModel;
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
                    Error(d["Passwords do not match."]);
                    return;
                }
                var valSta = pc.ValidatePassword(password);
                if (valSta.Code != 0)
                {
                    Error(valSta.Message);
                    return;
                }
                var newUser = pc.CreateLocalUser(user.Name, password);
                var db = newUser.GetDatabase();
                if (!db.Exists())
                {
                    Error(d["User's database does not exist. An empty database will be created."]);
                    db.Exists();
                }
                else // jeżeli katalog z plikami bazy danych istnieje, to odszyfrowujemy je starym hasłem
                {
                    var decSta = ProgressBarViewModel.ShowDialog(window,
                        d["Decrypting user's database."], true,
                        (worker, args) =>
                        pc.DecryptDirectory(new BackgroundProgress((BackgroundWorker)worker, args),
                            db.DirectoryPath,
                            pc.ComputeDigest(oldPassword, user.DBSalt),
                            user.DBInitializationVector));
                    if (decSta.Code == 1)
                    {
                        Error(d["Database decryption and password change canceled."]);
                        return;
                    }
                    else if (decSta.Code < 0)
                    {
                        Error(decSta.Message);
                        return;
                    }
                }
                // zaszyfrowujemy plik bazy danych nowym hasłem
                var encSta = ProgressBarViewModel.ShowDialog(window,
                    d["Encrypting user's database."], false,
                    (worker, args) =>
                    pc.EncryptDirectory(new BackgroundProgress((BackgroundWorker)worker, args),
                        db.DirectoryPath,
                        pc.ComputeDigest(password, newUser.DBSalt),
                        newUser.DBInitializationVector));
                if (encSta.Code == 1)
                {
                    Error(d["You should not have canceled database decryption. It may have been corrupted."]);
                    return;
                }
                if (encSta.Code != 0)
                {
                    Error(encSta.Message + " " + d["Database may have been corrupted."]);
                    return;
                }

                var updSta = lus.Update(user.Name, newUser);
                if (updSta.Code != 0)
                {
                    Error(updSta.Message);
                    return;
                }
                password.Dispose();
                confirmedPassword.Dispose();
                /* user.Salt = salt;
                user.Digest = digest; */
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
