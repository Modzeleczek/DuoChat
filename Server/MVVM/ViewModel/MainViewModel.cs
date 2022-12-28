using Shared.MVVM.Core;

namespace Server.MVVM.ViewModel
{
    public class MainViewModel : ViewModel
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
            var server = new Model.Server();

            Close = new RelayCommand(e => { });
            var setVM = new SettingsViewModel();
            setVM.ServerStart += () =>
            {
                
            };

            var conCliVM = new ConnectedClientsViewModel();
            var accVM = new AccountsViewModel();
            var tabs = new ViewModel[] { setVM,  conCliVM, accVM };
            SelectTab = new RelayCommand(e =>
            {
                int index = int.Parse((string)e);
                if (SelectedTab == tabs[index]) return;
                SelectedTab = tabs[index];
            });
        }
    }
}
