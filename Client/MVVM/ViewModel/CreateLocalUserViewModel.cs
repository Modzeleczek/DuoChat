using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
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
                var db = newUser.GetDatabase();
                if (db.Exists())
                {
                    Error(d["Database already exists and will be removed."]);
                    db.Delete();
                }
                db.Create();
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
