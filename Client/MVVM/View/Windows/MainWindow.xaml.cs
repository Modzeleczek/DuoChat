using Client.MVVM.ViewModel;
using Shared.MVVM.Core;
using Shared.MVVM.View.Windows;
using System.Windows;
using System.Windows.Controls;

namespace Client.MVVM.View.Windows
{
    public partial class MainWindow : DialogWindow
    {
        private bool serversAccountsVisible = false;

        public MainWindow(Window owner, MainViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();

        private void Button_ToggleServersAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (serversAccountsVisible)
            {
                ServersColumn.Width = new GridLength(0, GridUnitType.Pixel);
                AccountsColumn.Width = new GridLength(0, GridUnitType.Pixel);
                ToggleServersAccountsButton.Content = ">";
            }
            else
            {
                ServersColumn.Width = new GridLength(200, GridUnitType.Pixel);
                AccountsColumn.Width = new GridLength(200, GridUnitType.Pixel);
                ToggleServersAccountsButton.Content = "<";
            }
            serversAccountsVisible = !serversAccountsVisible;
        }

        private void Button_Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Button_Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
                ResizableGrid.Visibility = Visibility.Hidden;
            }
            else
                WindowState = WindowState.Normal;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /* potrzebne, aby po nieudanej próbie połączenia z serwerem,
            kliknięte konto na liście kont zostało odznaczone */
            var listView = (ListView)sender;
            if (listView.SelectedItem == null)
                listView.UnselectAll();
        }
    }
}
