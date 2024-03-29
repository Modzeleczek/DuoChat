﻿using Client.MVVM.Model;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.AccountActions
{
    public class AccountEditorViewModel : FormViewModel
    {
        #region Commands
        public RelayCommand GeneratePrivateKey { get; }
        #endregion

        #region Fields
        protected Storage _storage;
        #endregion

        protected AccountEditorViewModel(Storage storage)
        {
            _storage = storage;

            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e!;
                window = win;
                RequestClose += () => win.Close();
            });

            GeneratePrivateKey = new RelayCommand(e =>
            {
                var fields = (List<Control>)e!;

                var result = ProgressBarViewModel.ShowDialog(window!,
                    "|Private key generation|", true,
                    (reporter) => PrivateKey.Random(reporter));
                if (!(result is Success success)) return; // anulowano

                var textBox = (TextBox)fields[1];
                textBox.Text = ((PrivateKey)success.Data!).ToString();
            });
        }

        protected bool ValidateLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                Alert("|Login cannot be empty.|");
                return false;
            }

            if (Encoding.UTF8.GetBytes(login).Length > 255)
            {
                Alert("|Login encoded in UTF-8 must be at most 255 bytes long|.");
                return false;
            }

            // aby zapobiec SQLInjection, dopuszczamy tylko duże i małe litery oraz cyfry
            foreach (var c in login)
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                {
                    Alert("|Login may contain only letters and digits.|");
                    return false;
                }
            return true;
        }

        protected bool AccountExists(LocalUserPrimaryKey localUserKey,
            ServerPrimaryKey serverKey, string login)
        {
            try { return _storage.AccountExists(localUserKey, serverKey, login); }
            catch (Error e)
            {
                e.Prepend("|Could not| |check if| |account| |already exists.|");
                Alert(e.Message);
                throw;
            }
        }

        protected bool ParsePrivateKey(string text, out PrivateKey? privateKey)
        {
            privateKey = null;
            // klucz prywatny walidujemy jako ostatni, bo najdłużej to trwa
            var result = ProgressBarViewModel.ShowDialog(window!,
                "|Private key validation|", true,
                (reporter) => PrivateKey.Parse(reporter, text));

            // anulowano lub błąd (błędy zostały już wyświetlone w ProgressBarViewModelu)
            if (!(result is Success success))
                return false;

            // powodzenie
            privateKey = (PrivateKey)success.Data!;
            return true;
        }
    }
}
