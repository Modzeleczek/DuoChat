﻿using Client.MVVM.Model;
using Shared.MVVM.Core;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Security;
using Shared.MVVM.ViewModel.Results;
using Shared.MVVM.ViewModel;
using Shared.MVVM.View.Windows;

namespace Client.MVVM.ViewModel.LocalUsers
{
    public class LocalLoginViewModel : FormViewModel
    {
        public LocalLoginViewModel(Storage storage,
            LocalUserPrimaryKey localUserKey, bool returnEnteredPassword)
        {
            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e!;
                window = win;
                win.AddPasswordField("|Password|");
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls!;

                var localUser = storage.GetLocalUser(localUserKey);

                var password = ((PasswordBox)fields[0]).SecurePassword;
                if (!PasswordCryptography.DigestsEqual(
                    password, localUser.PasswordSalt, localUser.PasswordDigest))
                {
                    Alert("|Wrong password.|");
                    return;
                }
                SecureString? resultData = null;
                if (returnEnteredPassword)
                    resultData = password;
                else
                    password.Dispose();
                OnRequestClose(new Success(resultData));
            });

            var defaultCloseHandler = Close;
            Close = new RelayCommand(e =>
            {
                var fields = (List<Control>)e!;
                ((PasswordBox)fields[0]).SecurePassword.Dispose();
                defaultCloseHandler?.Execute(e);
            });
        }

        public static Result ShowDialog(Window owner, Storage storage,
            LocalUserPrimaryKey localUserKey, bool returnEnteredPassword, string? title = null)
        {
            var vm = new LocalLoginViewModel(storage, localUserKey, returnEnteredPassword);
            vm.Title = title ?? "|Enter your password|";
            vm.ConfirmButtonText = "|OK|";
            new FormWindow(owner, vm).ShowDialog();
            return vm.Result;
        }
    }
}
