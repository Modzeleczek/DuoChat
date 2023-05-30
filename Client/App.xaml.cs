using Client.MVVM.View.Windows;
using Client.MVVM.ViewModel;
using Client.Properties;
using System;
using System.Windows;

namespace Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            new MainWindow(null, new MainViewModel()).Show();
        }

        private ResourceDictionary ThemeDictionary => Resources.MergedDictionaries[0];

        public App()
        {
            InitializeComponent();

            switch (Settings.Default.CurrentTheme)
            {
                
                case "Light":
                    ChangeTheme(new Uri("/MVVM/View/Themes/LightTheme.xaml", UriKind.Relative));
                    Settings.Default.CurrentTheme = "Light";
                    Settings.Default.Save();
                    break;
                default:
                    ChangeTheme(new Uri("/MVVM/View/Themes/DarkTheme.xaml", UriKind.Relative));
                    Settings.Default.CurrentTheme = "Dark";
                    Settings.Default.Save();
                    break;
            }
        }
        public void ChangeTheme(Uri uri)
        {
            ThemeDictionary.MergedDictionaries.Clear();
            ThemeDictionary.MergedDictionaries.Add(new ResourceDictionary() { Source = uri });
        }

    }
}
