using Client.MVVM.ViewModel;
using Shared.MVVM.View.Windows;
using System;
using System.Windows;
using System.Windows.Input;

namespace Client.MVVM.View.Windows
{
    public partial class SettingsWindow : DialogWindow
    {
        public SettingsWindow(Window owner, SettingsViewModel dataContext)
            : base(owner, dataContext) { }

        protected override void Initialize() => InitializeComponent();

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        private void Themes_Click(object sender, RoutedEventArgs e)
        {
            if (Themes.IsChecked == true)
            {
                var app = (App)Application.Current;
                app.ChangeTheme(new Uri("MVVM/View/DynamicResources/Themes/Dark.xaml", UriKind.Relative));
                Properties.Settings.Default.CurrentTheme = "Dark";
                Properties.Settings.Default.Save();
            }
            else
            {
                var app = (App)Application.Current;
                app.ChangeTheme(new Uri("MVVM/View/DynamicResources/Themes/Light.xaml", UriKind.Relative));
                Properties.Settings.Default.CurrentTheme = "Light";
                Properties.Settings.Default.Save();
            }
        }
    }
}
