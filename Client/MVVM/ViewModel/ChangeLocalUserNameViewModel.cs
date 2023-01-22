using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.View.Windows;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class ChangeLocalUserNameViewModel : FormViewModel
    {
        public ChangeLocalUserNameViewModel(LocalUser user)
        {
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(e => window = (DialogWindow)e);
            Confirm = new RelayCommand(e =>
            {
                var inpCtrls = (Control[])e;
                var newUsername = ((TextBox)inpCtrls[0]).Text;
                var unValSta = lus.ValidateUserName(newUsername);
                if (unValSta.Code != 0)
                {
                    Alert(unValSta.Message);
                    return;
                }
                var existsStatus = lus.Exists(newUsername);
                if (existsStatus.Code < 0)
                {
                    existsStatus.Prepend(d["Error occured while"],
                       d["checking if"], d["user"], d["already exists."]);
                    Alert(existsStatus.Message);
                    return;
                }
                if (existsStatus.Code == 0)
                {
                    Alert(existsStatus.Message);
                    return;
                }
                var oldUserName = user.Name;
                user.Name = newUsername;
                var updateStatus = lus.Update(oldUserName, user);
                if (updateStatus.Code != 0)
                {
                    updateStatus.Prepend(d["Error occured while"], d["updating user in database."]);
                    Alert(updateStatus.Message);
                    return;
                }
                OnRequestClose(new Status(0));
            });
        }
    }
}
