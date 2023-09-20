using Shared.MVVM.Model.Cryptography;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;

namespace Client.MVVM.Model
{
    public static class PasswordCryptography
    {
        #region Fields
        private const int PASSWORD_SALT_LENGTH = 256 / 8,
            PASSWORD_DIGEST_LENGTH = 256 / 8;
        #endregion

        // TODO: przenieść do wspólnego viewmodelu LocalUserActions
        public static string ValidatePassword(SecureString password)
        {
            if (password == null)
                return "|Specify a password.|";
            IntPtr bstr = IntPtr.Zero;
            if (password.Length < 8)
                return "|Password should be at least 8 characters long.|";
            try
            {
                bstr = Marshal.SecureStringToBSTR(password);
                unsafe
                {
                    bool allWhiteSpace = true;
                    for (char* p = (char*)bstr.ToPointer(); *p != 0; ++p)
                        if (!char.IsWhiteSpace(*p))
                        { allWhiteSpace = false; break; }
                    if (allWhiteSpace)
                        return "|Specify a password.|";

                    bool hasDigit = false;
                    for (char* p = (char*)bstr.ToPointer(); *p != 0; ++p)
                        if (*p >= '0' && *p <= '9')
                        { hasDigit = true; break; }
                    if (!hasDigit)
                        return "|Password should contain at least one digit.|";

                    bool hasSpecial = false;
                    for (char* p = (char*)bstr.ToPointer(); *p != 0; ++p)
                        if (!((*p >= 'a' && *p <= 'z') || (*p >= 'A' && *p <= 'Z') ||
                            (*p >= '0' && *p <= '9')))
                        { hasSpecial = true; break; }
                    if (!hasSpecial)
                        return "|Password should contain at least one special character (not a letter or digit).|";
                }
                return null;
            }
            finally
            {
                if (bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
            }
        }

        public static bool SecureStringsEqual(SecureString ss1, SecureString ss2)
        {
            IntPtr bstr1 = IntPtr.Zero;
            IntPtr bstr2 = IntPtr.Zero;
            try
            {
                // https://stackoverflow.com/a/4502736/14357934
                bstr1 = Marshal.SecureStringToBSTR(ss1);
                bstr2 = Marshal.SecureStringToBSTR(ss2);
                unsafe
                {
                    char* p1 = (char*)bstr1.ToPointer(), p2 = (char*)bstr2.ToPointer();
                    for (; *p1 != 0 && *p2 != 0;
                         ++p1, ++p2)
                    {
                        if (*p1 != *p2)
                            return false;
                    }
                    // *p1 lub *p2 jest równe 0, ale oba nie są jednocześnie równe 0
                    /*
                    a     b     (a == 0 && b != 0) || (a != 0 && b == 0)
                    !=0   !=0   false
                    !=0   ==0   true  |a != b
                    ==0   !=0   true  |
                    ==0   ==0   false
                    */
                    if (*p1 != *p2)
                        return false;
                }
                return true;
            }
            finally // jest wykonywane przy wychodzeniu sterowania z bloku try poprzez standardowe wyjście, break, continue, goto, return, wyjątek
            {
                if (bstr1 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr1);
                if (bstr2 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr2);
            }
        }

        public static bool DigestsEqual(SecureString password, byte[] salt, byte[] digest)
        {
            var pasDig = ComputeDigest(password, salt, digest.Length);
            if (pasDig.Length != digest.Length)
                return false;

            for (int i = 0; i < pasDig.Length; ++i)
                if (pasDig[i] != digest[i])
                    return false;
            return true;
        }

        // https://stackoverflow.com/a/43858011/14357934
        public static byte[] ComputeDigest(SecureString password, byte[] salt, int digestLength)
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToBSTR(password);
                // czytamy 4-bajtową liczbę, której początek jest przesunięty o -4 względem ptr
                int length = Marshal.ReadInt32(ptr, -4);
                byte[] passwordByteArray = new byte[length];
                GCHandle handle = GCHandle.Alloc(passwordByteArray, GCHandleType.Pinned);
                try
                {
                    for (int i = 0; i < length; i++)
                        passwordByteArray[i] = Marshal.ReadByte(ptr, i);
                    // implementacja PBKDF2
                    using (var rfc2898 = new Rfc2898DeriveBytes(passwordByteArray,
                        salt, 10000, HashAlgorithmName.SHA256))
                        // Z hasła i soli uzyskujemy klucz AES.
                        return rfc2898.GetBytes(digestLength);
                }
                finally
                {
                    Array.Clear(passwordByteArray, 0, passwordByteArray.Length);
                    handle.Free();
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero) Marshal.ZeroFreeBSTR(ptr);
            }
        }

        public static (byte[], byte[]) GenerateSaltDigest(SecureString password)
        {
            var salt = RandomGenerator.Generate(PASSWORD_SALT_LENGTH);
            var digest = ComputeDigest(password, salt, PASSWORD_DIGEST_LENGTH);
            return (salt, digest);
        }
    }
}
