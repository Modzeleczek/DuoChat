using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Controls;
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
                var password = ((PreviewablePasswordBox)inpCtrls[1]).Password;
                var confirmedPasswordBox = (PreviewablePasswordBox)inpCtrls[2];
                /* if (userName == "")
                {
                    Error(d["Username cannot be empty."]);
                    confirmedPasswordBox.Password = "";
                    return;
                } */
                if (password != confirmedPasswordBox.Password)
                {
                    Error(d["Passwords do not match."]);
                    confirmedPasswordBox.Password = "";
                    return;
                }
                // if (!Validate(password)) return;
                if (LocalUsersStorage.Exists(userName))
                {
                    Error(d["User with name"] + $" {userName} " + d["already exists."]);
                    return;
                }
                var salt = Cryptography.GenerateSalt();
                var status = LocalUsersStorage.Add(new LocalUser(userName, salt,
                    Cryptography.ComputeDigest(password, salt)));
                if (status.Code != 0)
                {
                    Error(status.Message);
                    return;
                }
                OnRequestClose(new Status(0));
            });
        }
    }
}
