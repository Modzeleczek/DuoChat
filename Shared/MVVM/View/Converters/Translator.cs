using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace Shared.MVVM.View.Converters
{
    public abstract class Translator : IValueConverter
    {
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
        protected Dictionary<string, string>[] languages;

        protected Translator(Dictionary<string, string>[] languages)
        {
            this.languages = languages;
            FillDictionaries();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null) throw new ArgumentException("Parameter is null.");
            return TranslateWithActiveDictionary((string)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string TranslateWithActiveDictionary(string key)
        {
            if (activeLanguageId == 0) return key;
            var activeDict = languages[activeLanguageId - 1];
            if (activeDict.TryGetValue(key, out string translated)) return translated;
            else return "no translation";
        }

        public string this[string key] { get => TranslateWithActiveDictionary(key); }

        protected abstract void FillDictionaries();
    }
}
