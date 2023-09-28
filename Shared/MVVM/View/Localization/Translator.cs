using Shared.MVVM.Core;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Windows;

namespace Shared.MVVM.View.Localization
{
    public class Translator
    {
        public static Translator Instance { get; } = new Translator();

        public enum Language { English = 0, Polish = 1 }
        // domyślnie ustawiamy język angielski
        private Language _activeLanguage = Language.English;
        public Language ActiveLanguage
        {
            get => _activeLanguage;
            set
            {
                _activeLanguage = value;
                FillDictionary();
            }
        }
        public dynamic D { get; } = new ExpandoObject();

        private Translator()
        {
            FillDictionary();
        }

        public string this[string expression]
        {
            get
            {
                // Tłumaczymy tekst zapisany między znakami ||.
                var sb = new StringBuilder();
                var parser = new Parser();
                foreach (var c in expression)
                {
                    parser.Write(c);
                    if (parser.TextReady == Parser.Text.Normal)
                        sb.Append(parser.FlushText());
                    else if (parser.TextReady == Parser.Text.Translated)
                        sb.Append(Translate(parser.FlushText()));
                }
                /* Jeżeli ostatnim znakiem expression był |, to zostało wykonane
                Translate(parser.FlushText()) i poniższe FlushText zwróci "", bo
                bufor parsera jest pusty. W przeciwnym przypadku - jest to
                sytuacja, w której ostatnim fragmentem expression nie jest tekst
                do przetłumaczenia, ale normalny tekst - zostanie on dołączony
                do sb w poniższym wywołaniu. */
                sb.Append(parser.FlushText());
                return sb.ToString();
            }
        }

        private string Translate(string key)
        {
            var activeDict = (IDictionary<string, object>)D;
            string noTrans = "|No translation:|" + key;
            if (!activeDict.TryGetValue(key, out object obj))
                return noTrans;
            string translated = obj as string;
            if (translated is null)
                return noTrans;
            return translated;
        }

        public void ToggleLanguage()
        {
            ActiveLanguage = ActiveLanguage.Next();
        }

        public void SwitchLanguage(Language language)
        {
            ActiveLanguage = language;
        }

        private void FillDictionary()
        {
            var resDict = new ResourceDictionary();
            resDict.Source = new Uri("/MVVM/View/Localization/Translations.xaml",
                UriKind.Relative);
            var keys = resDict.Keys;
            var activeDict = (IDictionary<string, object>)D;
            foreach (string k in keys)
            {
                if (!(resDict[k] is Entry entry))
                    throw new InvalidCastException($"Key {k} is not of type Entry.");
                /* nie można używać indeksera na referencji typu ExpandoObject -
                trzeba jawnie zrzutować na IDictionary */
                if (ActiveLanguage == Language.English) // angielski
                {
                    /* jeżeli Entry nie ma w Translations.xaml ustawionego atrybutu EN,
                    to używamy klucza jako angielskiego tłumaczenia */
                    activeDict[k] = entry.EN ?? k;
                }
                else // polski
                {
                    activeDict[k] = entry.PL;
                }
            }
        }
    }
}
