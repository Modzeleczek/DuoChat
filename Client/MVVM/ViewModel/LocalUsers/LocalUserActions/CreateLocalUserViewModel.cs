using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.LocalUsers.LocalUserActions
{
    public class CreateLocalUserViewModel : FormViewModel
    {
        public CreateLocalUserViewModel(Storage storage)
        {
            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e!;
                window = win;
                win.AddTextField("|Username|");
                win.AddPasswordField("|Password|");
                win.AddPasswordField("|Confirm password|");
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls!;

                LocalUserPrimaryKey localUserKey;
                // Walidacja nazwy użytkownika
                try { localUserKey = new LocalUserPrimaryKey(((TextBox)fields[0]).Text); }
                catch (Error e)
                {
                    Alert(e.Message);
                    return;
                }

                var password = ((PasswordBox)fields[1]).SecurePassword;
                var confirmedPassword = ((PasswordBox)fields[2]).SecurePassword;
                if (!PasswordCryptography.SecureStringsEqual(password, confirmedPassword))
                {
                    Alert("|Passwords do not match.|");
                    return;
                }

                var passwordVal = PasswordCryptography.ValidatePassword(password);
                if (!(passwordVal is null))
                {
                    Alert(passwordVal);
                    return;
                }

                try
                {
                    if (storage.LocalUserExists(localUserKey))
                    {
                        Alert(LocalUsersStorage.AlreadyExistsMsg(localUserKey));
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
                var newUser = new LocalUser(localUserKey, password);
                try { storage.AddLocalUser(newUser); }
                catch (Error e)
                {
                    e.Prepend("|Could not| |add| |user to database.|");
                    Alert(e.Message);
                    throw;
                }

                // zaszyfrowujemy katalog użytkownika jego hasłem
                var encryptRes = ProgressBarViewModel.ShowDialog(window!,
                    "|Encrypting user's database.|", false,
                    (reporter) => storage.EncryptLocalUser(ref reporter,
                    newUser.GetPrimaryKey(), password));
                if (encryptRes is Cancellation)
                {
                    var e = new Error("|You should not have canceled database encryption. " +
                        "It may have been corrupted.|");
                    Alert(e.Message);
                    throw e;
                }
                else if (encryptRes is Failure failure)
                {
                    var e = failure.Reason;
                    e.Prepend("|Error occured while| " +
                        "|encrypting user's database.| |Database may have been corrupted.|");
                    Alert(e.Message);
                    throw e;
                }

                password.Dispose();
                confirmedPassword.Dispose();
                OnRequestClose(new Success(newUser));
            });

            var defaultCloseHandler = Close;
            Close = new RelayCommand(e =>
            {
                var fields = (List<Control>)e!;
                ((PasswordBox)fields[1]).SecurePassword.Dispose();
                ((PasswordBox)fields[2]).SecurePassword.Dispose();
                // odpowiednik base.Close w nadpisanej metodzie wirtualnej
                defaultCloseHandler?.Execute(e);
            });
        }
    }
}
