using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.LocalUsers.LocalUserActions
{
    public class ChangeLocalUserNameViewModel : FormViewModel
    {
        public ChangeLocalUserNameViewModel(Storage storage, LocalUserPrimaryKey localUserKey)
        {
            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                win.AddTextField("|Username|", localUserKey.Name);
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls;

                var localUser = storage.GetLocalUser(localUserKey);

                LocalUserPrimaryKey newLocalUserKey;
                // Walidacja nazwy użytkownika
                try { newLocalUserKey = new LocalUserPrimaryKey(((TextBox)fields[0]).Text); }
                catch (Error e)
                {
                    Alert(e.Message);
                    return;
                }

                try
                {
                    if (storage.LocalUserExists(newLocalUserKey))
                    {
                        Alert(LocalUsersStorage.AlreadyExistsMsg(newLocalUserKey));
                        return;
                    }
                }
                catch (Error e)
                {
                    e.Prepend("|Could not| |check if| |user| |already exists.|");
                    Alert(e.Message);
                    throw;
                }

                // użytkownik jeszcze nie istnieje
                localUser.SetPrimaryKey(newLocalUserKey);
                try { storage.UpdateLocalUser(localUserKey, localUser); }
                catch (Error e)
                {
                    e.Prepend("|Could not| |update| |user in database.|");
                    Alert(e.Message);
                    throw;
                }
                // Przekazujemy zaktualizowanego lokalnego użytkownika.
                OnRequestClose(new Success(localUser));
            });
        }
    }
}
