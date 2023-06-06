using System;

namespace Shared.MVVM.Core
{
    public static class ExtensionMethods
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }
}
