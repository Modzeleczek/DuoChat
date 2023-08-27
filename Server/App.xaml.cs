using Server.MVVM.View.Windows;
using Server.MVVM.ViewModel;
using Shared.MVVM.Core;
using System;
using System.Windows;

namespace Server
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            new MainWindow(null, new MainViewModel()).Show();
        }

        private ResourceDictionary ThemeDictionary => Resources.MergedDictionaries[0];

        public enum Theme { Dark, Light }
        // domyślnie ustawiamy ciemny motyw
        private Theme _activeTheme = Theme.Dark;
        public Theme ActiveTheme
        {
            get => _activeTheme;
            set
            {
                var uri = $"/MVVM/View/Resources/Dynamic/Themes/{value}.xaml";
                ThemeDictionary.MergedDictionaries.Clear();
                ThemeDictionary.MergedDictionaries.Add(
                    new ResourceDictionary() { Source = new Uri(uri, UriKind.Relative) });
                _activeTheme = value;
            }
        }

        public App()
        {
            InitializeComponent();
        }

        public void ToggleTheme()
        {
            ActiveTheme = ActiveTheme.Next();
        }
    }
}
