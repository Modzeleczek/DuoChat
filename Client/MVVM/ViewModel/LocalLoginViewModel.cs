using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using System.Windows;
using System.Windows.Controls;
using Shared.MVVM.Model;
using System.Collections.Generic;
using System.Security;

namespace Client.MVVM.ViewModel
{
    public class LocalLoginViewModel : FormViewModel
    {
        public LocalLoginViewModel(LocalUser user, bool returnEnteredPassword)
        {
            var pc = new PasswordCryptography();

            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                win.AddPasswordField("|Password|");
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                var password = ((PasswordBox)fields[0]).SecurePassword;
                if (!pc.DigestsEqual(password, user.PasswordSalt, user.PasswordDigest))
                {
                    Alert("|Wrong password.|");
                    return;
                }
                SecureString statusData = null;
                if (returnEnteredPassword)
                    statusData = password;
                else
                    password.Dispose();
                OnRequestClose(new Status(0, statusData));
            });

            var defaultCloseHandler = Close;
            Close = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;
                ((PasswordBox)fields[0]).SecurePassword.Dispose();
                defaultCloseHandler?.Execute(e);
            });
        }

        public static Status ShowDialog(Window owner,
            LocalUser user, bool returnEnteredPassword, string title = null)
        {
            var vm = new LocalLoginViewModel(user, returnEnteredPassword);
            vm.Title = title ?? "|Enter your password|";
            vm.ConfirmButtonText = "|OK|";
            new FormWindow(owner, vm).ShowDialog();
            return vm.Status;
        }
    }
}
