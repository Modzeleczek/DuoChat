using Client.MVVM.Core;
using Client.MVVM.Model;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class LocalUsersViewModel : ObservableObject
    {
        #region Commands
        public RelayCommand WindowLoaded { get; }
        public RelayCommand Create { get; }
        public RelayCommand UserDoubleClick { get; }
        #endregion

        #region Properties
        private ObservableCollection<LocalUser> localUsers;
        public ObservableCollection<LocalUser> LocalUsers
        { get => localUsers; set { localUsers = value; OnPropertyChanged(); } }
        #endregion

        private Window window;
        public event EventHandler OnRequestClose;

        public LocalUsersViewModel()
        {
            WindowLoaded = new RelayCommand(e => window = (Window)e);
            LocalUsers = new ObservableCollection<LocalUser>();
            for (int i = 0; i < 3; ++i)
                LocalUsers.Add(new LocalUser { Name = new string('a', i), PasswordDigest = null });
            UserDoubleClick = new RelayCommand(e =>
            {
                var user = (LocalUser)e;
                MessageBox.Show($"clicked {user.Name}");
            });
        }
    }
}
