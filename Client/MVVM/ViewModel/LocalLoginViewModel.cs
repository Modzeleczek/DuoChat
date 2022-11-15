using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Controls;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class LocalLoginViewModel : FormViewModel
    {
        public LocalLoginViewModel(LocalUser user)
        {
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var box = (PreviewablePasswordBox)inpCtrls[0];
                var password = box.Password;
                /* var status = LocalUsersStorage.Get(userName, out LocalUser user);
                if (status.Code != 0)
                {
                    Error(status.Message);
                    return;
                } */
                // implementacja PBKDF2
                if (!Cryptography.Compare(password, user.Salt, user.Digest))
                {
                    Error(d["Wrong password."]);
                    box.Password = "";
                    return;
                }
                var lu = LoggedUser.Instance;
                lu.LocalName = user.Name;
                lu.LocalPassword = password;
                OnRequestClose(new Status(0));
            });
        }
    }
}
