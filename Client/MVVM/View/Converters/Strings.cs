using Shared.MVVM.View.Converters;
using System.Collections.Generic;

namespace Client.MVVM.View.Converters
{
    public class Strings : Translator
    {
        public static Strings Instance { get; } = new Strings();

        private Strings() : base(new Dictionary<string, string>[1]) { }

        protected override void FillDictionaries()
        {
            FillPolish();
        }

        private void FillPolish()
        {
            languages[0] = new Dictionary<string, string>();
            var d = languages[0];
            d["Conversations"] = "Konwersacje";
            d["Servers"] = "Serwery";
            d["@message"] = "@wiadomość";
            d["Users"] = "Użytkownicy";
            d["Create user"] = "Stwórz użytkownika";
            d["Settings"] = "Ustawienia";
            d["User`apos;s nickname"] = "Pseudonim użytkownika";
            d["Nickname"] = "Pseudonim";
            d["Username"] = "Nazwa użytkownika";
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
            d["Password should contain at least one special character (not a letter or digit)."] = "Hasło powinno zawierać przynajmniej jeden znak specjalny (nie literę ani cyfrę).";
            d["Save"] = "Zapisz";
            d["New password"] = "Nowe hasło";
            d["Wrong password."] = "Nieprawidłowe hasło.";
            d["Log local user out"] = "Wyloguj lokalnego użytkownika";
            d["Dark theme"] = "Ciemny motyw";
            d["Confirm"] = "Zatwierdź";
            d["Decryption"] = "Odszyfrowywanie";
            d["Decrypting user's database."] = "Odszyfrowywanie bazy danych użytkownika.";
            d["User's database decrypted."] = "Baza danych użytkownika odszyfrowana.";
            d["No user is logged."] = "Żaden użytkownik nie jest zalogowany.";
            d["Database already exists and will be removed."] = "Baza danych już istnieje i zostanie usunięta.";
            d["File read timed out."] = "Przekroczono czas oczekiwania na odczyt pliku.";
            d["Error occured while reading file."] = "Wystąpił błąd podczas odczytywania pliku.";
            d["Error occured while writing file."] = "Wystąpił błąd podczas zapisywania pliku.";
            d["User's database does not exist. An empty database will be created."] = "Baza danych użytkownika nie istnieje. Stworzona zostanie pusta baza danych.";
            d["Encryption"] = "Szyfrowanie";
            d["Encrypting user's database."] = "Szyfrowanie bazy danych użytkownika.";
            d["Database encryption and user creation canceled."] = "Szyfrowanie bazy danych i tworzenie użytkownika anulowane.";
            d["Logged user does not exist."] = "Zalogowany użytkownik nie istnieje.";
            d["User's database is corrupted. An empty database will be created."] = "Baza danych użytkownika jest uszkodzona. Stworzona zostanie pusta baza danych.";
            d["Database decryption and password change canceled."] = "Odszyfrowywanie bazy danych i zmiana hasła anulowana.";
            d["Server with GUID"] = "Serwer z GUIDem";
            d["Progress"] = "Postęp";
            d["User's database decryption canceled. Logging out."] = "Odszyfrowywanie bazy danych użytkownika anulowane. Wylogowuję.";
            d["Server list may have been corrupted."] = "Lista serwerów mogła zostać uszkodzona.";
            d["You should not have canceled database decryption. It may have been corrupted."] = "Nie powinieneś anulować szyfrowania bazy danych. Mogła zostać uszkodzona.";
            d["Encrypting server's database."] = "Szyfrowanie bazy danych serwera.";
            d["User's database encryption canceled. Not logging out."] = "Szyfrowanie bazy danych użytkownika anulowane. Nie wylogowuję.";
            d["Accounts"] = "Konta";
            d["OK"] = "OK";
            d["Alert"] = "Alert";
            d["DuoChat"] = "DuoChat";
        }
    }
}
