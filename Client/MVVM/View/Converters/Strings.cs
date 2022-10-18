using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Client.MVVM.View.Converters
{
    public class Strings : IValueConverter
    {
        public static Strings Instance { get; } = new Strings();
        private int activeLanguageId = 1;
        public int ActiveLanguageId
        {
            get { return activeLanguageId; }
            set
            {
                if (value >= 0 && value < languages.Length + 1)
                    activeLanguageId = value;
                else throw new ArgumentOutOfRangeException(
                    $"Active language id must be in range <0, {languages.Length}>.");
            }
        }
        private Dictionary<string, string>[] languages = new Dictionary<string, string>[1];

        private Strings()
        {
            InitializePolish();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null) throw new ArgumentException("Parameter is null.");
            var key = (string)parameter;
            if (activeLanguageId == 0) return key;
            var activeDict = languages[activeLanguageId - 1];
            if (activeDict.TryGetValue(key, out string translated)) return translated;
            else return "no translation";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private void InitializePolish()
        {
            languages[0] = new Dictionary<string, string>();
            var d = languages[0];
            d["Friends"] = "Znajomi";
            d["Servers"] = "Serwery";
            d["@message"] = "@wiadomość";
            d["Users"] = "Użytkownicy";
            d["Create user"] = "Stwórz użytkownika";
            d["Settings"] = "Ustawienia";
            d["User&apos;s nickname"] = "Nazwa użytkownika";
            d["Nickname"] = "Nazwa użytkownika";
            d["DuoChat - settings"] = "DuoChat - ustawienia";
            d["Display"] = "Wyświetlanie";
            d["Profile"] = "Profil";
            d["Language"] = "Język";
        }   
    }
}
