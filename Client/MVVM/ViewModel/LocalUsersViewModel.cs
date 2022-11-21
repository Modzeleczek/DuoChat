using Client.MVVM.Core;
using Client.MVVM.Model;
using Client.MVVM.View.Windows;
using System.Collections.ObjectModel;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class LocalUsersViewModel : DialogViewModel
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
        { get => localUsers; set { localUsers = value; OnPropertyChanged(); } }
        #endregion

        public LocalUsersViewModel()
        {
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            LocalUsers = new ObservableCollection<LocalUser>(LocalUsersStorage.GetAll());
            Create = new RelayCommand(_ =>
            {
                var vm = new CreateLocalUserViewModel();
                var win = new FormWindow(window, vm, d["Create local user"], new FormWindow.Field[]
                {
                    new FormWindow.Field(d["Username"], "", false),
                    new FormWindow.Field(d["Password"], "", true),
                    new FormWindow.Field(d["Confirm password"], "", true)
                }, d["Create"]);
                vm.RequestClose += (s, e) => win.Close();
                win.ShowDialog();
                // ShowDialog blokuje wykonanie kod z tego obiektu typu command do momentu zamknięcia okna tworzenia użytkownika
                LocalUsers = new ObservableCollection<LocalUser>(LocalUsersStorage.GetAll());
            });
            Login = new RelayCommand(clickedUser =>
            {
                if (ShowLoginDialog((LocalUser)clickedUser))
                    OnRequestClose(new Status(0));
            });
            ChangeName = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                if (!ShowLoginDialog(user)) return; // nie udało się zalogować
                var vm = new ChangeLocalUserNameViewModel(user);
                var win = new FormWindow(window, vm, d["Change name"],
                    new FormWindow.Field[]
                    {
                        new FormWindow.Field(d["Username"], user.Name, false),
                    }, d["Save"]);
                vm.RequestClose += (s, e) => win.Close();
                win.ShowDialog();
                LocalUsers = new ObservableCollection<LocalUser>(LocalUsersStorage.GetAll());
            });
            ChangePassword = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                if (!ShowLoginDialog(user)) return;
                var vm = new ChangeLocalUserPasswordViewModel(user);
                var win = new FormWindow(window, vm, d["Change password"],
                    new FormWindow.Field[]
                    {
                        new FormWindow.Field(d["New password"], "", true),
                        new FormWindow.Field(d["Confirm password"], "", true)
                    }, d["Save"]);
                vm.RequestClose += (s, e) => win.Close();
                win.ShowDialog();
                LocalUsers = new ObservableCollection<LocalUser>(LocalUsersStorage.GetAll());
            });
            Delete = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                if (!ShowLoginDialog(user)) return;
                LocalUsersStorage.Delete(user.Name);
                LocalUsers = new ObservableCollection<LocalUser>(LocalUsersStorage.GetAll());
            });
        }

        private bool ShowLoginDialog(LocalUser user)
        {
            var vm = new LocalLoginViewModel(user);
            var win = new FormWindow(window, vm, d["Login"], new FormWindow.Field[]
            {
                new FormWindow.Field(d["Password"], "", true)
            }, d["Login"]);
            vm.RequestClose += (s, e) => win.Close();
            win.ShowDialog();
            if (vm.Status.Code != 0) return false; // nie udało się zalogować
            return true; // udało się zalogować
        }
    }
}
