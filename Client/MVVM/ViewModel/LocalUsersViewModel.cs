using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel.Observables;
using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using Shared.MVVM.ViewModel.Results;
using System.Collections.ObjectModel;
using System.Security;
using System.Windows;

namespace Client.MVVM.ViewModel
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

        public LocalUsersViewModel()
        {
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(windowLoadedE =>
            {
                /* musi być w WindowLoaded, bo musimy mieć przypisane window, kiedy
                chcemy otwierać nowe okno nad LocalUsersWindow */
                window = (DialogWindow)windowLoadedE;

                try { LocalUsers = new ObservableCollection<LocalUser>(lus.GetAll()); }
                catch (Error e)
                {
                    e.Prepend("|Error occured while| |reading user list.|");
                    Alert(e.Message);
                    throw;
                }
            });

            Create = new RelayCommand(_ =>
            {
                var vm = new CreateLocalUserViewModel
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
                var result = LocalLoginViewModel.ShowDialog(window, user, true,
                    "|Log in|");
                if (result is Success success)
                    OnRequestClose(new Success(new { LoggedUser = user, Password = success.Data }));
            });
            ChangeName = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                // nie udało się zalogować
                if (!(LocalLoginViewModel.ShowDialog(window, user, false) is Success)) return;
                // udało się zalogować
                var vm = new ChangeLocalUserNameViewModel(user)
                {
                    Title = "|Change_name|",
                    ConfirmButtonText = "|Save|"
                };
                new FormWindow(window, vm).ShowDialog();
                if (!(vm.Result is Success)) return;
                var index = LocalUsers.IndexOf(user);
                // index == -1 nie może wystąpić, bo user istnieje
                LocalUsers.RemoveAt(index);
                LocalUsers.Insert(index, user);
            });
            ChangePassword = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                var loginRes = LocalLoginViewModel.ShowDialog(window, user, true);
                if (!(loginRes is Success success)) return;
                var currentPassword = (SecureString)success.Data;
                var vm = new ChangeLocalUserPasswordViewModel(user, currentPassword)
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
                if (!(LocalLoginViewModel.ShowDialog(window, user, false) is Success)) return;
                try { lus.Delete(user.Name); }
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

        public static Result ShowDialog(Window owner)
        {
            var vm = new LocalUsersViewModel();
            var win = new LocalUsersWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Result;
        }
    }
}
