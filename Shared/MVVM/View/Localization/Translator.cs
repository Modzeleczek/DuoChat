using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Windows;

namespace Shared.MVVM.View.Localization
{
    public abstract class Translator
    {
        private int _activeLanguageId = 1;
        public dynamic D { get; } = new ExpandoObject();

        protected Translator()
        {
            FillDictionary();
        }

        public string this[string key]
        {
            get
            {
                var activeDict = (IDictionary<string, object>)D;
                const string noTrans = "no translation";
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
            if (_activeLanguageId == 1) // max
                _activeLanguageId = 0;
            else
                _activeLanguageId += 1;
            FillDictionary();
        }

        private void FillDictionary()
        {
            var resDict = new ResourceDictionary();
            resDict.Source = new Uri(TranslationsFilePath, UriKind.Relative);
            var keys = resDict.Keys;
            var activeDict = (IDictionary<string, object>)D;
            foreach (string k in keys)
            {
                if (!(resDict[k] is Entry entry))
                    throw new InvalidCastException($"Key {k} is not of type Entry.");
                // nie można używać indeksera na referencji typu ExpandoObject - trzeba jawnie zrzutować na IDictionary
                activeDict[k] = _activeLanguageId == 0 ? entry.EN : entry.PL;
            }
        }

        protected abstract string TranslationsFilePath { get; }
    }
}
