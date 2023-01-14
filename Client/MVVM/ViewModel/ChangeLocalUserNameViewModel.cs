using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
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
                var unValSta = lus.ValidateUserName(userName);
                if (unValSta.Code != 0)
                {
                    Alert(unValSta.Message);
                    return;
                }
                if (lus.Exists(userName))
                {
                    Alert(d["User with name"] + $" {userName} " + d["already exists."]);
                    return;
                }
                var newUser = new LocalUser(userName, user.PasswordSalt, user.PasswordDigest,
                    user.DBInitializationVector, user.DBSalt);
                var db = user.GetDatabase();
                if (!db.Exists())
                {
                    Alert(d["User's database does not exist. An empty database will be created."]);
                    db.Create();
                }
                var status = lus.Update(user.Name, newUser);
                if (status.Code != 0)
                {
                    Alert(status.Message);
                    return;
                }
                db.Rename(newUser.Name);
                // user.Name = userName;
                OnRequestClose(new Status(0));
            });
        }
    }
}
