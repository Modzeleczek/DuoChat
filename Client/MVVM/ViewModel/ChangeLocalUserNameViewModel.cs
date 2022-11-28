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
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var userName = ((TextBox)inpCtrls[0]).Text;
                if (lus.Exists(userName))
                {
                    Error(d["User with name"] + $" {userName} " + d["already exists."]);
                    return;
                }
                var newUser = new LocalUser(userName, user.PasswordSalt, user.PasswordDigest,
                    user.DBInitializationVector, user.DBSalt);
                var dao = newUser.GetDataAccessObject();
                if (!dao.DatabaseFileExists())
                {
                    Error(d["User's database does not exist. An empty database will be created."]);
                    dao.InitializeDatabaseFile();
                }
                var status = lus.Update(user.Name, newUser);
                if (status.Code != 0)
                {
                    Error(status.Message);
                    return;
                }
                dao.RenameDatabaseFile(newUser.Name);
                // user.Name = userName;
                OnRequestClose(new Status(0));
            });
        }
    }
}
