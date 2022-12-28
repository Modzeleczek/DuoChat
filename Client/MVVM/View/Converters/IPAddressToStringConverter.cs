using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Windows.Data;

namespace Client.MVVM.View.Converters
{
    public class IPAddressToStringConverter : IValueConverter
    {
        public IPAddressToStringConverter() { }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ip = value as IPAddress;
            if (ip == null)
                throw new ArgumentException("Value is not IPAddress.");
            if (ip.Address > int.MaxValue)
                throw new ArgumentException("Value is not valid IPv4 address.");
            int intIp = (int)(ip.Address >> (4 * 8));
            var octets = new byte[4];
            for (int i = 0; i <= 3; ++i)
            {
                octets[i] = (byte)(intIp & 255);
                intIp /= 256;
            }
            return string.Join(".", octets);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
