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
            return TrnslWthActDct((string)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string TrnslWthActDct(string key)
        {
            if (activeLanguageId == 0) return key;
            var activeDict = languages[activeLanguageId - 1];
            if (activeDict.TryGetValue(key, out string translated)) return translated;
            else return "no translation";
        }

        public static string Translate(string key) => Instance.TrnslWthActDct(key);

        public string this[string key] { get => TrnslWthActDct(key); }

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
            d["User`apos;s nickname"] = "Nazwa użytkownika";
            d["Nickname"] = d["Username"] = "Nazwa użytkownika";
            d["DuoChat - settings"] = "DuoChat - ustawienia";
            d["Display"] = "Wyświetlanie";
            d["Profile"] = "Profil";
            d["Language"] = "Język";
            d["Local users"] = "Lokalni użytkownicy";
            d["Create"] = "Stwórz";
            d["Edit"] = "Edytuj";
            d["Delete"] = "Usuń";
            d["Password"] = "Hasło";
            d["Login"] = "Zaloguj";
            d["Cancel"] = "Anuluj";
            d["Create local user"] = "Stwórz lokalnego użytkownika";
            d["Confirm password"] = "Potwierdź hasło";
            d["Change name"] = "Zmień nazwę";
            d["Change password"] = "Zmień hasło";
            d["Delete local user"] = "Usuń lokalnego użytkownika";
            d["Username cannot be empty."] = "Nazwa użytkownika nie może być pusta.";
            d["Passwords do not match."] = "Hasła nie są zgodne.";
            d["User with name"] = "Użytkownik o nazwie";
            d["already exists."] = "już istnieje.";
            d["does not exist."] = "nie istnieje.";
            d["Specify a password."] = "Określ hasło.";
            d["Password should be at least 8 characters long."] = "Hasło powinno mieć przynajmniej 8 znaków.";
            d["Password should contain at least one digit."] = "Hasło powinno zawierać przynajmniej jedną cyfrę.";
            d["Password should contain at least one special character (not a letter or a digit)."] = "Hasło powinno zawierać przynajmniej jeden znak specjalny (nie literę ani cyfrę).";
            d["Save"] = "Zapisz";
            d["New password"] = "Nowe hasło";
            d["Wrong password."] = "Nieprawidłowe hasło.";
            d["Log local user out"] = "Wyloguj lokalnego użytkownika";
        }
    }
}
