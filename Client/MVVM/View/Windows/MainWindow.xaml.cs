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

        protected override void Initialize()
        {
            InitializeComponent();
            InitializeColumns();
        }

        private void Button_ToggleServersAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (serversAccountsVisible)
                HideColumns();
            else
                ShowColumns();
            serversAccountsVisible = !serversAccountsVisible;
        }

        private void InitializeColumns()
        {
            SetWidth(ref ConversationsColumn, 0.16);
            HideColumns();
        }

        private void HideColumns()
        {
            SetWidth(ref ServersColumn, 0);
            SetWidth(ref AccountsColumn, 0);
            SetWidth(ref ChatColumn, 0.84);
            ToggleServersAccountsButton.Content = ">";
        }

        private void ShowColumns()
        {
            SetWidth(ref ServersColumn, 0.16);
            SetWidth(ref AccountsColumn, 0.16);
            SetWidth(ref ChatColumn, 0.52);
            ToggleServersAccountsButton.Content = "<";
        }

        private void SetWidth(ref ColumnDefinition column, double value)
        {
            column.Width = new GridLength(value, GridUnitType.Star);
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
