using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Windows;

namespace Shared.MVVM.View.Localization
{
    public class Translator
    {
        public static Translator Instance { get; } = new Translator();
        private int _activeLanguageId = 0;
        public int ActiveLanguageId
        {
            get => _activeLanguageId;
            set
            {
                if (!(value >= 0 && value <= 1))
                    throw new ArgumentOutOfRangeException(
                        $"Active language id must be in range <0,1>.");
                _activeLanguageId = value;
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
            if (ActiveLanguageId == 1) // max
                ActiveLanguageId = 0;
            else
                ActiveLanguageId += 1;
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
                activeDict[k] = ActiveLanguageId == 0 ? entry.EN : entry.PL;
            }
        }
    }
}
