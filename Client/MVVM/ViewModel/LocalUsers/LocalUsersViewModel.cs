using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.LocalUsers.LocalUserActions;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel;
using Shared.MVVM.ViewModel.Results;
using System.Collections.ObjectModel;
using System.Security;
using System.Windows;

namespace Client.MVVM.ViewModel.LocalUsers
{
    public class LocalUsersViewModel : WindowViewModel
    {
        #region Commands
        public RelayCommand Create { get; }
        public RelayCommand Login { get; }
        public RelayCommand ChangeName { get; }
        public RelayCommand ChangePassword { get; }
        public RelayCommand Delete { get; }
        #endregion

        #region Properties
        private ObservableCollection<LocalUser> localUsers;
        public ObservableCollection<LocalUser> LocalUsers
        {
            get => localUsers;
            set { localUsers = value; OnPropertyChanged(); }
        }
        #endregion

        public LocalUsersViewModel(Storage storage)
        {
            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                /* musi być w WindowLoaded, bo musimy mieć przypisane window, kiedy
                chcemy otwierać nowe okno nad LocalUsersWindow */
                window = (DialogWindow)windowLoadedE;

                try { LocalUsers = new ObservableCollection<LocalUser>(storage.GetAllLocalUsers()); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |reading user list.|");
                    Alert(e.Message);
                    throw;
                }
            });

            Create = new RelayCommand(_ =>
            {
                var vm = new CreateLocalUserViewModel(storage)
                {
                    Title = "|Create local user|",
                    ConfirmButtonText = "|Create|"
                };
                new FormWindow(window, vm).ShowDialog();
                /* ShowDialog blokuje wykonanie kodu z tego obiektu typu command
                do momentu zamknięcia okna tworzenia użytkownika */
                if (vm.Result is Success success)
                    LocalUsers.Add((LocalUser)success.Data);
            });

            Login = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                var result = LocalLoginViewModel.ShowDialog(window, storage,
                    user.GetPrimaryKey(), true, "|Log in|");
                if (result is Success success)
                    OnRequestClose(new Success(new { LoggedUser = user, Password = success.Data }));
            });

            ChangeName = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                var loginRes = LocalLoginViewModel.ShowDialog(window, storage,
                    user.GetPrimaryKey(), false);
                // nie udało się zalogować
                if (!(loginRes is Success))
                    return;
                // udało się zalogować
                var vm = new ChangeLocalUserNameViewModel(storage, user.GetPrimaryKey())
                {
                    Title = "|Change_name|",
                    ConfirmButtonText = "|Save|"
                };
                new FormWindow(window, vm).ShowDialog();
                if (vm.Result is Success success)
                {
                    var updatedUser = (LocalUser)success.Data;
                    updatedUser.CopyTo(user);
                }
            });

            ChangePassword = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                var loginRes = LocalLoginViewModel.ShowDialog(window, storage,
                    user.GetPrimaryKey(), true);
                if (!(loginRes is Success success))
                    return;
                var currentPassword = (SecureString)success.Data;
                var vm = new ChangeLocalUserPasswordViewModel(storage,
                    user.GetPrimaryKey(), currentPassword)
                {
                    Title = "|Change_password|",
                    ConfirmButtonText = "|Save|"
                };
                new FormWindow(window, vm).ShowDialog();
                currentPassword.Dispose();
            });

            Delete = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                var loginRes = LocalLoginViewModel.ShowDialog(window, storage, user.GetPrimaryKey(), false);
                if (!(loginRes is Success))
                    return;
                try { storage.DeleteLocalUser(user.GetPrimaryKey()); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |deleting| " +
                        "|user from database.|");
                    Alert(e.Message);
                    throw;
                }
                LocalUsers.Remove(user);
            });
        }

        public static Result ShowDialog(Window owner, Storage storage)
        {
            var vm = new LocalUsersViewModel(storage);
            var win = new LocalUsersWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Result;
        }
    }
}
