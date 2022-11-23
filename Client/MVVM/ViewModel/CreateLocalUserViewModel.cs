using Client.MVVM.Core;
using Client.MVVM.Model;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class CreateLocalUserViewModel : PasswordFormViewModel
    {
        public CreateLocalUserViewModel()
        {
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
                    password.Clear();
                    confirmedPassword.Clear();
                    return;
                } */
                if (!SecureStringsEqual(password, confirmedPassword))
                {
                    Error(d["Passwords do not match."]);
                    password.Clear();
                    confirmedPassword.Clear();
                    return;
                }
                if (!Validate(password)) return;
                if (LocalUsersStorage.Exists(userName))
                {
                    Error(d["User with name"] + $" {userName} " + d["already exists."]);
                    return;
                }
                var salt = GenerateSalt();
                var digest = ComputeDigest(password, salt);
                password.Dispose();
                confirmedPassword.Dispose();
                var status = LocalUsersStorage.Add(new LocalUser(userName, salt, digest));
                if (status.Code != 0)
                {
                    Error(status.Message);
                    return;
                }
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
