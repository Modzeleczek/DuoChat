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
                var oldUserName = user.Name;
                var renameStatus = user.Rename(userName);
                if (renameStatus.Code != 0)
                {
                    Alert(renameStatus.Message);
                    return;
                }
                var updateStatus = lus.Update(oldUserName, user.ToSerializable());
                if (updateStatus.Code != 0)
                {
                    Alert(updateStatus.Message);
                    return;
                }
                OnRequestClose(new Status(0));
            });
        }
    }
}
