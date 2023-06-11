using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using Shared.MVVM.Model;
using Shared.MVVM.View.Windows;
using System.Collections.Generic;
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
                window = (DialogWindow)windowLoadedE;

                /* musi być w WindowLoaded, bo musimy mieć przypisane window, kiedy
                chcemy otwierać nowe okno nad LocalUsersWindow */
                var getAllStatus = lus.GetAll();
                if (getAllStatus.Code != 0)
                {
                    getAllStatus.Prepend("|Error occured while| |reading user list.|");
                    Alert(getAllStatus.Message);
                    LocalUsers = new ObservableCollection<LocalUser>();
                }
                else
                    LocalUsers = new ObservableCollection<LocalUser>((List<LocalUser>)getAllStatus.Data);
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
                var status = vm.Status;
                if (status.Code == 0)
                    LocalUsers.Add((LocalUser)status.Data);
            });
            Login = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                var status = LocalLoginViewModel.ShowDialog(window, user, true,
                    "|Log in|");
                if (status.Code != 0) return;
                OnRequestClose(new Status(0, new { LoggedUser = user, Password = status.Data }));
            });
            ChangeName = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                // nie udało się zalogować
                if (LocalLoginViewModel.ShowDialog(window, user, false).Code != 0) return;
                // udało się zalogować
                var vm = new ChangeLocalUserNameViewModel(user)
                {
                    Title = "|Change_name|",
                    ConfirmButtonText = "|Save|"
                };
                new FormWindow(window, vm).ShowDialog();
                if (vm.Status.Code != 0)
                    return;
                var index = LocalUsers.IndexOf(user);
                // index == -1 nie może wystąpić, bo user istnieje
                LocalUsers.RemoveAt(index);
                LocalUsers.Insert(index, user);
            });
            ChangePassword = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                var loginStatus = LocalLoginViewModel.ShowDialog(window, user, true);
                if (loginStatus.Code != 0) return;
                var currentPassword = (SecureString)loginStatus.Data;
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
                if (LocalLoginViewModel.ShowDialog(window, user, false).Code != 0) return;
                var status = lus.Delete(user.Name);
                if (status.Code != 0)
                {
                    status.Prepend("|Error occured while| |deleting| " +
                        "|user from database.|");
                    Alert(status.Message);
                    return;
                }
                LocalUsers.Remove(user);
            });
        }

        public static Status ShowDialog(Window owner)
        {
            var vm = new LocalUsersViewModel();
            var win = new LocalUsersWindow(owner, vm);
            vm.RequestClose += () => win.Close();
            win.ShowDialog();
            return vm.Status;
        }
    }
}
