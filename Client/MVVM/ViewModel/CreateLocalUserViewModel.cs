using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.View.Windows;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public class CreateLocalUserViewModel : FormViewModel
    {
        public CreateLocalUserViewModel()
        {
            var pc = new PasswordCryptography();
            var lus = new LocalUsersStorage();

            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                win.AddTextField(d["Username"]);
                win.AddPasswordField(d["Password"]);
                win.AddPasswordField(d["Confirm password"]);
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;

                var userName = ((TextBox)fields[0]).Text;
                var unValSta = LocalUsersStorage.ValidateUserName(userName);
                if (unValSta.Code != 0)
                {
                    Alert(unValSta.Message);
                    return;
                }

                var password = ((PasswordBox)fields[1]).SecurePassword;
                var confirmedPassword = ((PasswordBox)fields[2]).SecurePassword;
                if (!pc.SecureStringsEqual(password, confirmedPassword))
                {
                    Alert(d["Passwords do not match."]);
                    return;
                }
                var valSta = pc.ValidatePassword(password);
                if (valSta.Code != 0)
                {
                    Alert(valSta.Message);
                    return;
                }

                var existsStatus = lus.Exists(userName);
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
                // użytkownik jeszcze nie istnieje
                var newUser = new LocalUser(userName, password);
                var addStatus = lus.Add(newUser);
                if (addStatus.Code != 0)
                {
                    addStatus.Prepend(d["Error occured while"], d["adding"], d["user to database."]);
                    Alert(addStatus.Message);
                    return;
                }
                password.Dispose();
                confirmedPassword.Dispose();
                OnRequestClose(new Status(0, newUser));
            });

            var defaultCloseHandler = Close;
            Close = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;
                ((PasswordBox)fields[1]).SecurePassword.Dispose();
                ((PasswordBox)fields[2]).SecurePassword.Dispose();
                // odpowiednik base.Close w nadpisanej metodzie wirtualnej
                defaultCloseHandler?.Execute(e);
            });
        }
    }
}
