using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Converters;
using Client.MVVM.View.Windows;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class LocalLoginViewModel : PasswordFormViewModel
    {
        public LocalLoginViewModel(LocalUser user, bool returnEnteredPassword)
        {
            var pc = new PasswordCryptography();
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var password = ((PasswordBox)inpCtrls[0]).SecurePassword;
                if (!pc.DigestsEqual(password, user.PasswordSalt, user.PasswordDigest))
                {
                    Error(d["Wrong password."]);
                    return;
                }
                object retDat = null;
                if (returnEnteredPassword)
                    retDat = new { LoggedUser = user, Password = password };
                else
                {
                    password.Dispose();
                    retDat = user;
                }
                OnRequestClose(new Status(0, null, retDat));
            });
        }

        protected override void DisposePasswords(Control[] controls)
        {
            ((PasswordBox)controls[0]).SecurePassword.Dispose();
        }

        public static Status Dialog(Window owner,
            LocalUser user, bool returnEnteredPassword)
        {
            var d = Strings.Instance;
            var vm = new LocalLoginViewModel(user, returnEnteredPassword);
            var win = new FormWindow(owner, vm, d["Login"], new FormWindow.Field[]
            {
                new FormWindow.Field(d["Password"], "", true)
            }, d["Cancel"], d["Login"]);
            vm.RequestClose += (s, e) => win.Close();
            win.ShowDialog();
            return vm.Status;
        }
    }
}
