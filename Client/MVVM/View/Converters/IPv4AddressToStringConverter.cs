using Shared.MVVM.Model.Networking;
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
            var ipAddress = value as IPv4Address;
            if (ipAddress == null)
                throw new ArgumentException("Value is not of type IPv4Address.");
            return ipAddress.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = IPv4Address.TryParse((string)value);
            if (status.Code != 0) return null;
            return (IPv4Address)status.Data;
        }
    }
}
