using System;
using System.Text;

namespace Shared.MVVM.Core
{
    public static class ExtensionMethods
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        // iterowanie po wartościach enuma (https://stackoverflow.com/a/643438)
        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(
                String.Format("Argument {0} is not an Enum", typeof(T).FullName));

            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }

        #region Byte array
        public static string ToHexString(this byte[] array, int startIndex, int count)
        {
            var sb = new StringBuilder();
            for (int i = startIndex; i < startIndex + count; ++i)
                sb.Append($"{array[i]:X2} ");
            return sb.ToString();
        }

        public static string ToHexString(this byte[] array)
        {
            return ToHexString(array, 0, array.Length);
        }

        public static byte[] Slice(this byte[] array, int startIndex, int count)
        {
            byte[] ret = new byte[count];
            Buffer.BlockCopy(array, startIndex, ret, 0, count);
            return ret;
        }

        public static byte[] Slice(this byte[] array, int startIndex)
        {
            return array.Slice(startIndex, array.Length - startIndex);
        }

        public static byte[] ToBytes(this string hex)
        {
            if (hex.Length % 3 != 0)
                throw new ArgumentException("hex must be a sequence of groups consisting " +
                    "of 2 hex digits delimited with space.", nameof(hex));

            byte[] bytes = new byte[hex.Length / 3];
            for (int i = 0; i < bytes.Length; ++i)
                bytes[i] = (byte)((HexValue(hex[i * 3]) << 4) + (HexValue(hex[(i * 3) + 1])));
            return bytes;
        }

        private static int HexValue(char hex)
        {
            // Tylko dla cyfr 0-9 i dużych liter A-F.
            if (hex <= '0' + 9)
                return hex - '0';
            else
                return hex - 'A' + 10;
        }
        #endregion
    }
}
