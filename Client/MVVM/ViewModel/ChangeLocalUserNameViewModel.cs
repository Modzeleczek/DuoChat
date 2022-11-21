using Client.MVVM.Core;
using Client.MVVM.Model;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class ChangeLocalUserNameViewModel : FormViewModel
    {
        public ChangeLocalUserNameViewModel(LocalUser user)
        {
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var userName = ((TextBox)inpCtrls[0]).Text;
                if (LocalUsersStorage.Exists(userName))
                {
                    Error(d["User with name"] + $" {userName} " + d["already exists."]);
                    return;
                }
                var status = LocalUsersStorage.Update(user.Name,
                    new LocalUser(userName, user.Salt, user.Digest));
                if (status.Code != 0)
                {
                    Error(status.Message);
                    return;
                }
                // user.Name = userName;
                OnRequestClose(new Status(0));
            });
        }
    }
}
