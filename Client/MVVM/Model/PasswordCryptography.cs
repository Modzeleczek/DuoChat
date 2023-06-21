using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Client.MVVM.Model
{
    public class PasswordCryptography
    {
        public string ValidatePassword(SecureString password)
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

        public bool SecureStringsEqual(SecureString ss1, SecureString ss2)
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

        public bool DigestsEqual(SecureString password, byte[] salt, byte[] digest)
        {
            var pasDig = ComputeDigest(password, salt);
            for (int i = 0; i < pasDig.Length; ++i)
                if (pasDig[i] != digest[i])
                    return false;
            return true;
        }

        // https://stackoverflow.com/a/43858011/14357934
        public byte[] ComputeDigest(SecureString password, byte[] salt)
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
                        // z hasła i soli uzyskujemy 128-bitowy klucz
                        return rfc2898.GetBytes(128 / 8);
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

        // TODO: wydzielić wszystko poniżej do nowej klasy (np. FileEncryptor)
        public void EncryptFile(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector)
        {
            var result = EncryptSingleFile(reporter, path, key, initializationVector);
            FileTransformationCleanup(reporter, result, path);
        }

        public void DecryptFile(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector)
        {
            var result = EncryptSingleFile(reporter, path, key, initializationVector);
            FileTransformationCleanup(reporter, result, path);
        }

        private Result EncryptSingleFile(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector)
        {
            using (var aes = CreateAes())
            using (var enc = aes.CreateEncryptor(key, initializationVector))
            using (var inFS = File.OpenRead(path))
            using (var outFS = File.OpenWrite(path + ".temp"))
            using (var cs = new CryptoStream(outFS, enc, CryptoStreamMode.Write))
            {
                reporter.FineMax = inFS.Length;
                return TransformFile(reporter, inFS, cs);
            }
        }

        private Result DecryptSingleFile(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector)
        {
            using (var aes = CreateAes())
            using (var dec = aes.CreateDecryptor(key, initializationVector))
            using (var inFS = File.OpenRead(path))
            using (var outFS = File.OpenWrite(path + ".temp"))
            using (var cs = new CryptoStream(inFS, dec, CryptoStreamMode.Read))
            {
                reporter.FineMax = inFS.Length;
                return TransformFile(reporter, cs, outFS);
            }
        }

        private Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            return aes;
        }

        // https://stackoverflow.com/a/32437759/14357934
        private Result TransformFile(ProgressReporter reporter, Stream readFrom, Stream writeTo)
        {
            const int bufferSize = 4096;
            reporter.FineProgress = 0;
            byte[] buffer = new byte[bufferSize];
            while (true)
            {
                // Pozycja w buforze.
                int position = 0;
                // Zapisujemy całkowitą liczbę bajtów pozostałych do odczytania.
                int remainingBytes = bufferSize; // rozmiar bufora
                int bytesRead = 0;
                int timeoutCounter = 0;
                // Dopóki liczby bajtów pozostałych do odczytania i bajtów odczytanych w aktualnej iteracji są niezerowe.
                // Jeżeli w 1000 iteracji nie uda się zapełnić całego bufora, to przerywamy.
                while (true)
                {
                    if (timeoutCounter == 1000)
                        return new Failure("|File read timed out.|");
                    if (remainingBytes == 0) break;
                    try
                    { bytesRead = readFrom.Read(buffer, position, remainingBytes); }
                    catch (Exception e)
                    { return new Failure(e, "|Error occured while reading file.|"); }
                    if (bytesRead == 0) break;
                    // O liczbę bajtów odczytanych w aktualnej iteracji zmniejszamy liczbę pozostałych bajtów i
                    remainingBytes -= bytesRead;
                    // przesuwamy pozycję w buforze.
                    position += bytesRead;
                    ++timeoutCounter;
                }
                position = 0;
                remainingBytes = bufferSize - remainingBytes; // zapisujemy całkowitą liczbę odczytanych bajtów, która zawsze jest mniejsza lub równa rozmiarowi bufora
                try
                { writeTo.Write(buffer, position, remainingBytes); }
                catch (Exception e)
                { return new Failure(e, "|Error occured while writing file.|"); }
                reporter.FineProgress += remainingBytes;
                // int progress = (int)(((double)bytesProcessed / inStreamSize) * 100.0);
                if (reporter.CancellationPending)
                    return new Cancellation();
                // Jeżeli bez problemów doszliśmy do końca pliku (EOF)
                // Przerywamy pętlę while (true).
                if (bytesRead == 0)
                    return new Success();
            }
        }

        private void FileTransformationCleanup(ProgressReporter reporter,
            Result result, string path)
        {
            if (!(result is Failure) && reporter.CancellationPending)
                result = new Cancellation();

            var tempPath = path + ".temp";
            // if (result is Failure || result is Cancellation)
            if (!(result is Success))
            {
                try { File.Delete(tempPath); }
                catch (Exception e) { result = new Failure(e, FileDeleteError(tempPath)); }
            }
            else
            {
                // usuwamy stary plik
                try { File.Delete(path); }
                catch (Exception e)
                {
                    result = new Failure(e, FileDeleteError(path));
                    goto FINISH;
                }

                // zmieniamy tymczasową nazwę nowego na nazwę starego
                try { File.Move(tempPath, path); }
                catch (Exception e) { result = new Failure(e, FileMoveError(tempPath, path)); }
            }
        FINISH:
            reporter.Result = result;
        }

        private string FileDeleteError(string path) =>
            $"|Error occured while| |deleting| |file| {path}.";

        private string FileMoveError(string source, string destination) =>
            $"|Error occured while| |moving| |file| {source} to {destination}.";

        public void EncryptDirectory(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector) =>
            TransformDirectory(reporter, path, key, initializationVector, EncryptSingleFile);

        public void DecryptDirectory(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector) =>
            TransformDirectory(reporter, path, key, initializationVector, DecryptSingleFile);

        private delegate Result FileTransformation(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector);

        private void TransformDirectory(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector,
            FileTransformation transformation)
        {
            var files = Directory.GetFiles(path);
            reporter.CoarseMax = files.Length - 1;
            reporter.CoarseProgress = 0;
            Result result = new Success();
            for (int i = 0; i < files.Length; ++i)
            {
                if (reporter.CancellationPending)
                {
                    result = new Cancellation();
                    goto DIRECTORY_CLEANUP;
                }
                result = transformation(reporter, files[i], key, initializationVector);
                if (!(result is Success))
                    goto DIRECTORY_CLEANUP;
                reporter.CoarseProgress += 1;
            }
        DIRECTORY_CLEANUP:
            DirectoryTransformationCleanup(reporter, result, files);
        }

        private void DirectoryTransformationCleanup(ProgressReporter reporter,
            Result result, string[] files)
        {
            if (!(result is Failure) && reporter.CancellationPending)
                result = new Cancellation();

            LinkedList<Error> errors = new LinkedList<Error>();
            if (!(result is Success))
            {
                // jeżeli użytkownik anulował lub wystąpił błąd, usuwamy nowe pliki
                foreach (var f in files)
                {
                    var temp = f + ".temp";
                    if (File.Exists(temp))
                    {
                        try { File.Delete(temp); }
                        catch (Exception e) { errors.AddLast(new Error(e, FileDeleteError(temp))); }
                    }
                }
            }
            else
            {
                foreach (var f in files)
                {
                    var temp = f + ".temp";
                    // usuwamy stary plik
                    try { File.Delete(f); }
                    catch (Exception e)
                    {
                        errors.AddLast(new Error(e, FileDeleteError(f)));
                        continue;
                    }

                    // zmieniamy tymczasową nazwę nowego na nazwę starego
                    try { File.Move(temp, f); }
                    catch (Exception e) { errors.AddLast(new Error(e, FileMoveError(temp, f))); }
                }
            }

            if (errors.Count > 0)
                result = new Failure(MergeErrorMessages(errors));

            reporter.Result = result;
        }

        private string MergeErrorMessages(LinkedList<Error> errors)
        {
            var sb = new StringBuilder();
            foreach (var e in errors)
            {
                sb.Append(e.Message);
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
