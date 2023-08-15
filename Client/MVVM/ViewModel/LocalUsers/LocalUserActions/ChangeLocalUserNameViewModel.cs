using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.LocalUsers.LocalUserActions
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

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls;

                var newUsername = ((TextBox)fields[0]).Text;
                var userNameVal = LocalUsersStorage.ValidateUserName(newUsername);
                if (!(userNameVal is null))
                {
                    Alert(userNameVal);
                    return;
                }

                try
                {
                    if (lus.Exists(newUsername))
                    {
                        Alert($"|User with name| {newUsername} |already exists.|");
                        return;
                    }
                }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |checking if| |user|" +
                        "|already exists.|");
                    Alert(e.Message);
                    throw;
                }

                // użytkownik jeszcze nie istnieje
                var oldUserName = user.Name;
                user.Name = newUsername;
                try { lus.Update(oldUserName, user); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |updating| |user in database.|");
                    Alert(e.Message);
                    throw;
                }
                OnRequestClose(new Success());
            });
        }
    }
}
