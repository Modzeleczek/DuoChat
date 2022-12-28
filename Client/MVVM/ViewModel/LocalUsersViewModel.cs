using Client.MVVM.Model;
using Client.MVVM.Model.BsonStorages;
using Client.MVVM.View.Windows;
using Shared.MVVM.Core;
using System.Collections.ObjectModel;
using System.Security;
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
        {
            get => localUsers;
            set { localUsers = value; OnPropertyChanged(); }
        }
        #endregion

        public LocalUsersViewModel()
        {
            var lus = new LocalUsersStorage();
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            LocalUsers = new ObservableCollection<LocalUser>(lus.GetAll());
            Create = new RelayCommand(_ =>
            {
                var vm = new CreateLocalUserViewModel();
                var win = new FormWindow(window, vm, d["Create local user"], new FormWindow.Field[]
                {
                    new FormWindow.Field(d["Username"], "", false),
                    new FormWindow.Field(d["Password"], "", true),
                    new FormWindow.Field(d["Confirm password"], "", true)
                }, d["Cancel"], d["Create"]);
                vm.RequestClose += (s, e) => win.Close();
                win.ShowDialog();
                // ShowDialog blokuje wykonanie kodu z tego obiektu typu command do momentu zamknięcia okna tworzenia użytkownika
                LocalUsers = new ObservableCollection<LocalUser>(lus.GetAll());
            });
            Login = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                var status = LocalLoginViewModel.ShowDialog(window, user, true, d["Login"]);
                if (status.Code != 0) return;
                OnRequestClose(status);
            });
            ChangeName = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                if (LocalLoginViewModel.ShowDialog(window, user, false).Code != 0) return; // nie udało się zalogować
                // udało się zalogować
                var vm = new ChangeLocalUserNameViewModel(user);
                var win = new FormWindow(window, vm, d["Change name"],
                    new FormWindow.Field[]
                    {
                        new FormWindow.Field(d["Username"], user.Name, false),
                    }, d["Cancel"], d["Save"]);
                vm.RequestClose += (s, e) => win.Close();
                win.ShowDialog();
                LocalUsers = new ObservableCollection<LocalUser>(lus.GetAll());
            });
            ChangePassword = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                var logSta = LocalLoginViewModel.ShowDialog(window, user, true);
                if (logSta.Code != 0) return;
                var curPas = (SecureString)((dynamic)logSta.Data).Password;
                var vm = new ChangeLocalUserPasswordViewModel(user, curPas);
                var win = new FormWindow(window, vm, d["Change password"],
                    new FormWindow.Field[]
                    {
                        new FormWindow.Field(d["New password"], "", true),
                        new FormWindow.Field(d["Confirm password"], "", true)
                    }, d["Cancel"], d["Save"]);
                vm.RequestClose += (s, e) => win.Close();
                win.ShowDialog();
                curPas.Dispose();
                LocalUsers = new ObservableCollection<LocalUser>(lus.GetAll());
            });
            Delete = new RelayCommand(clickedUser =>
            {
                var user = (LocalUser)clickedUser;
                if (LocalLoginViewModel.ShowDialog(window, user, false).Code != 0) return;
                var sta = lus.Delete(user.Name);
                if (sta.Code != 0)
                {
                    Error(sta.Message);
                    return;
                }
                user.GetDatabase().Delete();
                LocalUsers = new ObservableCollection<LocalUser>(lus.GetAll());
            });
        }
    }
}
