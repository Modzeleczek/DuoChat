using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace Client.MVVM.View.Converters
{
    public class FilePathToNameConverter : IValueConverter
    {
        public static FilePathToNameConverter Instance { get; } = new FilePathToNameConverter();

        private FilePathToNameConverter() { }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            if (path is null)
                throw new ArgumentException("Value is not of type string.");
            return Path.GetFileName(path);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
