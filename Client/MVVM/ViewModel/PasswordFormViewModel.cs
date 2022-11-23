using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public abstract class PasswordFormViewModel : FormViewModel
    {
        protected override void CancelHandler(object e)
        {
            DisposePasswords((Control[])e);
            base.CancelHandler(e);
            /* alternatywa z RTTI
            var inpCtrls = (Control[])e;
            foreach (var c in inpCtrls)
            {
                var p = c as PasswordBox;
                if (p != null)
                    p.SecurePassword.Dispose();
            }
            base.CancelHandler(e); */
        }

        protected abstract void DisposePasswords(Control[] controls);

        protected bool Validate(SecureString password)
        {
            if (password == null)
            { Error(d["Specify a password."]); return false; }
            IntPtr bstr = IntPtr.Zero;
            if (password.Length < 8)
            { Error(d["Password should be at least 8 characters long."]); return false; }
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
                    { Error(d["Specify a password."]); return false; }

                    bool hasDigit = false;
                    for (char* p = (char*)bstr.ToPointer(); *p != 0; ++p)
                        if (*p >= '0' && *p <= '9')
                        { hasDigit = true; break; }
                    if (!hasDigit)
                    { Error(d["Password should contain at least one digit."]); return false; }

                    bool hasSpecial = false;
                    for (char* p = (char*)bstr.ToPointer(); *p != 0; ++p)
                        if (!((*p >= 'a' && *p <= 'z') || (*p >= 'A' && *p <= 'Z') ||
                            (*p >= '0' && *p <= '9')))
                        { hasSpecial = true; break; }
                    if (!hasSpecial)
                    {
                        Error(d["Password should contain at least one special character (not a letter or digit)."]);
                        return false;
                    }
                }
                return true;
            }
            finally
            {
                if (bstr != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr);
            }
        }

        protected bool SecureStringsEqual(SecureString ss1, SecureString ss2)
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

        protected bool DigestsEqual(SecureString password, byte[] salt, byte[] digest)
        {
            var pasDig = ComputeDigest(password, salt);
            for (int i = 0; i < pasDig.Length; ++i)
                if (pasDig[i] != digest[i])
                    return false;
            return true;
        }

        // https://stackoverflow.com/a/43858011/14357934
        protected byte[] ComputeDigest(SecureString password, byte[] salt)
        {
            IntPtr ptr = Marshal.SecureStringToBSTR(password);
            try
            {
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
                        return rfc2898.GetBytes(16);
                }
                finally
                {
                    Array.Clear(passwordByteArray, 0, passwordByteArray.Length);
                    handle.Free();
                }
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptr);
            }
        }

        protected void GenerateRandom(byte[] bytes)
        {
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
        }

        protected byte[] GenerateSalt()
        {
            var bytes = new byte[16];
            GenerateRandom(bytes);
            return bytes;
        }
    }
}
