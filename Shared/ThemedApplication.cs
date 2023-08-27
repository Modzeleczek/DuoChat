using Shared.MVVM.Core;
using System;
using System.Windows;

namespace Shared
{
    public class ThemedApplication : Application
    {
        private ResourceDictionary ThemeDictionary => Resources.MergedDictionaries[0];

        public enum Theme { Dark, Light }
        // domyślnie ustawiamy ciemny motyw
        private Theme _activeTheme = Theme.Dark;
        public Theme ActiveTheme
        {
            get => _activeTheme;
            set
            {
                var uri = $"/MVVM/View/DynamicResources/Themes/{value}.xaml";
                ThemeDictionary.MergedDictionaries.Clear();
                ThemeDictionary.MergedDictionaries.Add(
                    new ResourceDictionary() { Source = new Uri(uri, UriKind.Relative) });
                _activeTheme = value;
            }
        }

        public void ToggleTheme()
        {
            ActiveTheme = ActiveTheme.Next();
        }
    }
}
