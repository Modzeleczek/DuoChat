using Client.MVVM.Model;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Client.MVVM.View.Converters
{
    public class IPv4AddressToStringConverter : IValueConverter
    {
        public IPv4AddressToStringConverter() { }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ip = value as IPv4Address;
            if (ip == null)
                throw new ArgumentException("Value is not IPv4Address.");
            return ip.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!IPv4Address.TryParse((string)value, out IPv4Address ret))
                return null;
            return ret;
        }
    }
}
