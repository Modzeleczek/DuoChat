using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Controls;
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
                var password = ((PreviewablePasswordBox)inpCtrls[0]).Password;
                var confirmedPasswordBox = (PreviewablePasswordBox)inpCtrls[1];
                if (password != confirmedPasswordBox.Password)
                {
                    Error(d["Passwords do not match."]);
                    confirmedPasswordBox.Password = "";
                    return;
                }
                // if (!Validate(password)) return;
                var salt = Cryptography.GenerateSalt();
                var digest = Cryptography.ComputeDigest(password, salt);
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
    }
}
