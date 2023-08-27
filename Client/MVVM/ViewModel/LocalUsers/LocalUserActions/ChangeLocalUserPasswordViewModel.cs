using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System.Collections.Generic;
using System.Security;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel.LocalUsers.LocalUserActions
{
    public class ChangeLocalUserPasswordViewModel : FormViewModel
    {
        public ChangeLocalUserPasswordViewModel(Storage storage,
            LocalUserPrimaryKey localUserKey, SecureString oldPassword)
        {
            var pc = new PasswordCryptography();

            WindowLoaded = new RelayCommand(e =>
            {
                var win = (FormWindow)e;
                window = win;
                win.AddPasswordField("|New password|");
                win.AddPasswordField("|Confirm password|");
                RequestClose += () => win.Close();
            });

            Confirm = new RelayCommand(controls =>
            {
                var fields = (List<Control>)controls;

                /* Wyrzuci wyjątek, który zamknie aplikację, jeżeli
                użytkownik nie istnieje, bo np. został ręcznie (niezależnie
                od naszej aplikacji) usunięty z bazy (ze struktury katalogów)
                między chwilą otwarcia okna zmiany nazwy a chwilą kliknięcia
                przycisku potwierdzenia. */
                var localUser = storage.GetLocalUser(localUserKey);

                var password = ((PasswordBox)fields[0]).SecurePassword;
                var confirmedPassword = ((PasswordBox)fields[1]).SecurePassword;
                if (!pc.SecureStringsEqual(password, confirmedPassword))
                {
                    Alert("|Passwords do not match.|");
                    return;
                }

                var passwordVal = pc.ValidatePassword(password);
                if (!(passwordVal is null))
                {
                    Alert(passwordVal);
                    return;
                }

                // Odszyfrowujemy katalog użytkownika starym hasłem.
                var decryptRes = ProgressBarViewModel.ShowDialog(window,
                    "|Decrypting user's database.|", true,
                    (reporter) => storage.DecryptLocalUser(ref reporter, localUserKey, oldPassword));
                if (decryptRes is Cancellation)
                    return;
                else if (decryptRes is Failure failure)
                {
                    var e = failure.Reason;
                    // nie udało się odszyfrować katalogu użytkownika, więc crashujemy program
                    e.Prepend("|Error occured while| |decrypting user's database.|");
                    Alert(e.Message);
                    throw e;
                }

                // wyznaczamy nową sól i skrót hasła oraz IV i sól bazy danych
                localUser.ResetPassword(password);
                try { storage.UpdateLocalUser(localUserKey, localUser); }
                /* TODO: UpdateLocalUser i inne metody ze Storage powinny rzucać
                jakiś UndoError, jeżeli po wystąpieniu Errora nie uda się cofnąć
                zmian do poprawnego stanu bazy, czyli np. w UpdateLocalUser jest to
                sytuacja, w której drugie wywołanie _localUsersStorage.Update wyrzuci
                undoError. UndoError powinien natychmiast przerwać wykonywanie programu,
                a zwykłe Errory powinny być takimi, które można naprawić (czyli przywrócić
                bazę do poprawnego stanu). */
                catch (Error updateError)
                {
                    updateError.Prepend("|Could not| |update| |user in database.|");

                    try { EncryptLocalUser(storage, localUserKey, password); }
                    catch (Error encryptError)
                    {
                        encryptError.Prepend("|Could not| |encrypt| |local user's database|.");
                        updateError.Append(encryptError.Message);
                    }
                    Alert(updateError.Message);
                    throw;
                }

                try { EncryptLocalUser(storage, localUserKey, password); }
                catch (Error encryptError)
                {
                    encryptError.Prepend("|Could not| |encrypt| |local user's database|.");
                    Alert(encryptError.Message);
                    throw;
                }

                password.Dispose();
                confirmedPassword.Dispose();
                OnRequestClose(new Success());
            });

            var defaultCloseHandler = Close;
            Close = new RelayCommand(e =>
            {
                var fields = (List<Control>)e;
                ((PasswordBox)fields[0]).SecurePassword.Dispose();
                ((PasswordBox)fields[1]).SecurePassword.Dispose();
                defaultCloseHandler?.Execute(e);
            });
        }

        private void TryEncryptLocalUser(Storage storage, ref Error updateError,
            LocalUserPrimaryKey localUserKey, SecureString password)
        {
            try { EncryptLocalUser(storage, localUserKey, password); }
            catch (Error encryptError)
            {
                encryptError.Prepend("|Could not| |encrypt| |local user's database|.");
                updateError.Append(encryptError.Message);
            }
        }

        private void EncryptLocalUser(Storage storage,
            LocalUserPrimaryKey localUserKey, SecureString password)
        {
            // zaszyfrowujemy katalog użytkownika nowym hasłem
            var encryptRes = ProgressBarViewModel.ShowDialog(window,
                "|Encrypting user's database.|", false,
                (reporter) => storage.EncryptLocalUser(ref reporter, localUserKey, password));

            if (encryptRes is Cancellation)
            {
                var e = new Error("|You should not have canceled database encryption. " +
                    "It may have been corrupted.|");
                throw e;
            }
            else if (encryptRes is Failure failure)
            {
                var e = failure.Reason;
                e.Prepend("|Database may have been corrupted.|");
                throw e;
            }
        }
    }
}
