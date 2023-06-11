using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class ChangeLocalUserNameViewModel : FormViewModel
    {
        public ChangeLocalUserNameViewModel(LocalUser user)
        {
            var lus = new LocalUsersStorage();

            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                win.AddTextField("|Username|", user.Name);
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                var newUsername = ((TextBox)fields[0]).Text;
                var unValSta = LocalUsersStorage.ValidateUserName(newUsername);
                if (unValSta.Code != 0)
                {
                    Alert(unValSta.Message);
                    return;
                }

                var existsStatus = lus.Exists(newUsername);
                if (existsStatus.Code < 0)
                {
                    existsStatus.Prepend("|Error occured while| |checking if| |user|" +
                        "|already exists.|");
                    Alert(existsStatus.Message);
                    return;
                }
                if (existsStatus.Code == 0)
                {
                    Alert(existsStatus.Message);
                    return;
                }
                // użytkownik nie istnieje

                var oldUserName = user.Name;
                user.Name = newUsername;
                var updateStatus = lus.Update(oldUserName, user);
                if (updateStatus.Code != 0)
                {
                    user.Name = oldUserName;
                    updateStatus.Prepend("|Error occured while| |updating |user in database.|");
                    Alert(updateStatus.Message);
                    return;
                }
                OnRequestClose(new Status(0));
            });
        }
    }
}
