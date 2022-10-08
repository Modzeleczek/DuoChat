using System.Windows;
using System.Windows.Input;

namespace Client
{
    public partial class MainWindow : Window
    {
        private bool serversVisible = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Button_ToggleServers_Click(object sender, RoutedEventArgs e)
        {
            if (serversVisible)
            {
                ServersColumn.Width = new GridLength(0, GridUnitType.Pixel);
                ToggleServersButton.Content = ">";
            }
            else
            {
                ServersColumn.Width = new GridLength(200, GridUnitType.Pixel);
                ToggleServersButton.Content = "<";
            }
            serversVisible = !serversVisible;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Closing -= Window_Closing;
            Close();
        }

        private void Button_Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Button_Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState != WindowState.Maximized)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }
    }
}
