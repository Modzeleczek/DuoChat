using Client.Core;
using System;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class UsersViewModel : ObservableObject
    {
        private Window window;

        public RelayCommand WindowLoaded { get; }
        public RelayCommand Create { get; }

        public event EventHandler OnRequestClose;

        public UsersViewModel()
        {
            WindowLoaded = new RelayCommand(e => window = (Window)e);
        }
    }
}
