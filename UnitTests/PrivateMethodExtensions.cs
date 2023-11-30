using System.Reflection;

namespace UnitTests
{
    internal static class PrivateMethodExtensions
    {
        // https://github.com/microsoft/testfx/issues/366#issuecomment-1110639841
        private static T Invoke<T>(this object obj, string methodName, BindingFlags staticOrInstance,
            params object[] parameters)
        {
            // Type.GetType zwraca RuntimeType czyli typ typu.
            if (!(obj is Type type))
                type = obj.GetType();

            MethodInfo? method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | staticOrInstance);
            if (method == null)
                throw new ArgumentException($"No method \"{methodName}\" found in class " +
                    $"\"{type.Name}\""); // Zamiast Name może być AssemblyQualifiedName.

            // Kiedy wywołujemy metodę statyczną, pierwszy parametr jest ignorowany.
            object? res = method.Invoke(obj, parameters);
            if (res is T)
                return (T)res;

            string prefix = $"Bad return type. \"{typeof(T).Name}\" was passed, " +
                $"whereas the actual method result is ";
            if (res is null)
                throw new ArgumentException(prefix + "null");

            throw new ArgumentException(prefix + $"of type \"{res.GetType().Name}\"");
        }

        public static T InvokeStatic<T>(this object obj, string methodName, params object[] parameters)
        {
            return obj.Invoke<T>(methodName, BindingFlags.Static, parameters);
        }

        public static T InvokeInstance<T>(this object obj, string methodName, params object[] parameters)
        {
            return obj.Invoke<T>(methodName, BindingFlags.Instance, parameters);
        }
    }
}
