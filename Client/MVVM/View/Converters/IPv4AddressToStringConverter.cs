using Shared.MVVM.Core;
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
            try { return IPv4Address.Parse((string)value); }
            catch (Error) { return null; }
        }
    }
}
