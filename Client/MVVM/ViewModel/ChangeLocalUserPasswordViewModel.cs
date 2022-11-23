using Client.MVVM.Core;
using Client.MVVM.Model;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class ChangeLocalUserPasswordViewModel : PasswordFormViewModel
    {
        public ChangeLocalUserPasswordViewModel(LocalUser user)
        {
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var password = ((PasswordBox)inpCtrls[0]).SecurePassword;
                var confirmedPassword = ((PasswordBox)inpCtrls[1]).SecurePassword;
                if (!SecureStringsEqual(password, confirmedPassword))
                {
                    Error(d["Passwords do not match."]);
                    password.Clear();
                    confirmedPassword.Clear();
                    return;
                }
                if (!Validate(password)) return;
                var salt = GenerateSalt();
                var digest = ComputeDigest(password, salt);
                password.Dispose();
                confirmedPassword.Dispose();
                var status = LocalUsersStorage.Update(user.Name,
                    new LocalUser(user.Name, salt, digest));
                if (status.Code != 0)
                {
                    Error(status.Message);
                    return;
                }
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
