using Client.MVVM.Model;
using Shared.MVVM.View.Localization;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using System.Windows;
using System.Windows.Controls;
using Shared.MVVM.Model;
using Shared.MVVM.View.Windows;
using Client.MVVM.Model.BsonStorages;

namespace Client.MVVM.ViewModel
{
    public class LocalLoginViewModel : PasswordFormViewModel
    {
        public LocalLoginViewModel(LocalUser user, bool returnEnteredPassword)
        {
            var pc = new PasswordCryptography();
            WindowLoaded = new RelayCommand(e => window = (DialogWindow)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var password = ((PasswordBox)inpCtrls[0]).SecurePassword;
                if (!pc.DigestsEqual(password, user.PasswordSalt, user.PasswordDigest))
                {
                    Alert(d["Wrong password."]);
                    return;
                }
                dynamic statusData = null;
                if (returnEnteredPassword)
                    statusData = new { LoggedUser = user, Password = password };
                else
                {
                    password.Dispose();
                    statusData = user;
                }
                OnRequestClose(new Status(0, statusData));
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
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Status;
        }
    }
}
