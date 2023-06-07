using Shared.MVVM.Core;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Windows;

namespace Shared.MVVM.View.Localization
{
    public class Translator
    {
        public static Translator Instance { get; } = new Translator();

        public enum Language { English, Polish }
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

        public string this[string key]
        {
            get
            {
                var activeDict = (IDictionary<string, object>)D;
                string noTrans = "no translation " + key;
                if (!activeDict.TryGetValue(key, out object obj))
                    return noTrans;
                string translated = obj as string;
                if (translated == null)
                    return noTrans;
                return translated;
            }
        }

        public void ToggleLanguage()
        {
            ActiveLanguage = ActiveLanguage.Next();
        }

        private void FillDictionary()
        {
            var resDict = new ResourceDictionary();
            resDict.Source = new Uri("/MVVM/View/Localization/Translations.xaml", UriKind.Relative);
            var keys = resDict.Keys;
            var activeDict = (IDictionary<string, object>)D;
            foreach (string k in keys)
            {
                if (!(resDict[k] is Entry entry))
                    throw new InvalidCastException($"Key {k} is not of type Entry.");
                // nie można używać indeksera na referencji typu ExpandoObject - trzeba jawnie zrzutować na IDictionary
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
