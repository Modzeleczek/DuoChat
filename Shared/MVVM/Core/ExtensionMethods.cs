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
        #endregion
    }
}
