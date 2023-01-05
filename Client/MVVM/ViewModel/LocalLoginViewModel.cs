using Client.MVVM.Model;
using Shared.MVVM.View.Localization;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using System.Windows;
using System.Windows.Controls;
using Shared.MVVM.Model;

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
                    Alert(d["Wrong password."]);
                    return;
                }
                object retDat = null;
                var db = user.GetDatabase();
                if (!db.Exists())
                {
                    Alert(d["User's database does not exist. An empty database will be created."]);
                    db.Create();
                }
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

        public static Status ShowDialog(Window owner,
            LocalUser user, bool returnEnteredPassword, string title = null)
        {
            var d = Translator.Instance;
            string finalTitle = title != null ? title : d["Enter your password"];
            var vm = new LocalLoginViewModel(user, returnEnteredPassword);
            var win = new FormWindow(owner, vm, finalTitle, new FormWindow.Field[]
            {
                new FormWindow.Field(d["Password"], "", true)
            }, d["Cancel"], d["OK"]);
            vm.RequestClose += (s, e) => win.Close();
            win.ShowDialog();
            return vm.Status;
        }
    }
}
