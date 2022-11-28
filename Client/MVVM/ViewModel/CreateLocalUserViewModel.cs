using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class CreateLocalUserViewModel : PasswordFormViewModel
    {
        public CreateLocalUserViewModel()
        {
            var pc = new PasswordCryptography();
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var userName = ((TextBox)inpCtrls[0]).Text;
                var password = ((PasswordBox)inpCtrls[1]).SecurePassword;
                var confirmedPassword = ((PasswordBox)inpCtrls[2]).SecurePassword;
                /* if (userName == "")
                {
                    Error(d["Username cannot be empty."]);
                    return;
                } */
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
                if (lus.Exists(userName))
                {
                    Error(d["User with name"] + $" {userName} " + d["already exists."]);
                    return;
                }
                var newUser = pc.CreateLocalUser(userName, password);
                var dao = newUser.GetDataAccessObject();
                if (dao.DatabaseFileExists())
                {
                    Error(d["Database already exists and will be removed."]);
                    dao.DeleteDatabaseFile();
                }
                dao.InitializeDatabaseFile();

                var encSta = ProgressBarViewModel.ShowDialog(window,
                    d["Encryption"],
                    d["Encrypting user's database."],
                    (sender, args) =>
                    pc.EncryptDatabase((BackgroundWorker)sender, args, newUser, password));
                if (encSta.Code == 1)
                {
                    dao.DeleteDatabaseFile();
                    Error(d["Database encryption and user creation canceled."]);
                    return;
                }
                if (encSta.Code < 0)
                {
                    dao.DeleteDatabaseFile();
                    Error(encSta.Message);
                    return;
                }

                var addSta = lus.Add(newUser);
                if (addSta.Code != 0)
                {
                    Error(addSta.Message);
                    return;
                }
                password.Dispose();
                confirmedPassword.Dispose();
                OnRequestClose(new Status(0));
            });
        }

        protected override void DisposePasswords(Control[] controls)
        {
            ((PasswordBox)controls[1]).SecurePassword.Dispose();
            ((PasswordBox)controls[2]).SecurePassword.Dispose();
        }
    }
}
