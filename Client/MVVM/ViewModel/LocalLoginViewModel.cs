using Client.MVVM.Core;
using Client.MVVM.Model;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class LocalLoginViewModel : PasswordFormViewModel
    {
        public LocalLoginViewModel(LocalUser user)
        {
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var password = ((PasswordBox)inpCtrls[0]).SecurePassword;
                if (!DigestsEqual(password, user.Salt, user.Digest))
                {
                    Error(d["Wrong password."]);
                    password.Clear();
                    return;
                }
                password.Dispose();
                var ret = new LoggedUser(user);
                OnRequestClose(new Status(0, null, ret));
            });
        }

        protected override void DisposePasswords(Control[] controls)
        {
            ((PasswordBox)controls[0]).SecurePassword.Dispose();
        }
    }
}
