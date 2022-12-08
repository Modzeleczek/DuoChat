using Shared.MVVM.Core;

namespace Server.MVVM.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        #region Commands
        // setujemy te właściwości w konstruktorze MainViewModel, a nie w callbacku (RelayCommandzie) zdarzenia WindowLoaded, więc nie potrzeba setterów z wywołaniami OnPropertyChanged
        public RelayCommand Close { get; }
        public RelayCommand SelectTab { get; }
        #endregion

        #region Properties
        private ViewModel selectedTab;
        public ViewModel SelectedTab
        {
            get => selectedTab;
            set { selectedTab = value; OnPropertyChanged(); }
        }
        #endregion

        public MainViewModel()
        {
            Close = new RelayCommand(e => { });
            var tabs = new ViewModel[]
            {
                new SettingsViewModel(),
                new ConnectedClientsViewModel(),
                new AccountsViewModel()
            };
            SelectTab = new RelayCommand(e =>
            {
                int index = (int)e;
                if (SelectedTab == tabs[index]) return;
                SelectedTab = tabs[index];
            });
        }
    }
}
