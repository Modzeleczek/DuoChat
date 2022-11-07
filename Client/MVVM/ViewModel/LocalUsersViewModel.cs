using Client.MVVM.Core;
using System;
using System.Windows;

namespace Client.MVVM.ViewModel
{
    public class LocalUsersViewModel : ObservableObject
    {
        #region Commands
        public RelayCommand WindowLoaded { get; }
        public RelayCommand Create { get; }
        #endregion

        #region Properties
        #endregion

        private Window window;
        public event EventHandler OnRequestClose;

        public LocalUsersViewModel()
        {
            WindowLoaded = new RelayCommand(e => window = (Window)e);
        }
    }
}
