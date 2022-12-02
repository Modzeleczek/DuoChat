﻿using Client.MVVM.View.Converters;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;

namespace Client.MVVM.Model
{
    public class PasswordCryptography
    {
        private readonly Strings d = Strings.Instance;

        public Status ValidatePassword(SecureString password)
        {
            if (password == null)
                return new Status(-1, d["Specify a password."]);
            IntPtr bstr = IntPtr.Zero;
            if (password.Length < 8)
                return new Status(-2, d["Password should be at least 8 characters long."]);
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
                        return new Status(-3, d["Specify a password."]);

                    bool hasDigit = false;
                    for (char* p = (char*)bstr.ToPointer(); *p != 0; ++p)
                        if (*p >= '0' && *p <= '9')
                        { hasDigit = true; break; }
                    if (!hasDigit)
                        return new Status(-4, d["Password should contain at least one digit."]);

                    bool hasSpecial = false;
                    for (char* p = (char*)bstr.ToPointer(); *p != 0; ++p)
                        if (!((*p >= 'a' && *p <= 'z') || (*p >= 'A' && *p <= 'Z') ||
                            (*p >= '0' && *p <= '9')))
                        { hasSpecial = true; break; }
                    if (!hasSpecial)
                        return new Status(-5, d["Password should contain at least one special character (not a letter or digit)."]);
                }
                return new Status(0);
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

        private void GenerateRandom(byte[] bytes)
        {
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
        }

        public byte[] GenerateRandom(int byteCount)
        {
            var bytes = new byte[byteCount];
            GenerateRandom(bytes);
            return bytes;
        }

        public LocalUser CreateLocalUser(string userName, SecureString password)
        {
            var passSalt = GenerateRandom(128 / 8);
            var passDigest = ComputeDigest(password, passSalt);
            var dbIv = GenerateRandom(128 / 8);
            var dbSalt = GenerateRandom(128 / 8);
            return new LocalUser(userName, passSalt, passDigest, dbIv, dbSalt);
        }

        public void EncryptFile(BackgroundProgress progress,
            string path, byte[] key, byte[] initializationVector)
        {
            var status = EncryptSingleFile(progress, path, key, initializationVector);
            FileTransformationCleanup(progress, status, path);
        }

        public void DecryptFile(BackgroundProgress progress,
            string path, byte[] key, byte[] initializationVector)
        {
            var status = EncryptSingleFile(progress, path, key, initializationVector);
            FileTransformationCleanup(progress, status, path);
        }
        
        private Status EncryptSingleFile(BackgroundProgress progress,
            string path, byte[] key, byte[] initializationVector)
        {
            using (var aes = CreateAes())
            using (var enc = aes.CreateEncryptor(key, initializationVector))
            using (var inFS = File.OpenRead(path))
            using (var outFS = File.OpenWrite(path + ".temp"))
            using (var cs = new CryptoStream(outFS, enc, CryptoStreamMode.Write))
            {
                progress.FineMax = inFS.Length;
                return TransformFile(progress, inFS, cs);
            }
        }
            
        private Status DecryptSingleFile(BackgroundProgress progress,
            string path, byte[] key, byte[] initializationVector)
        {
            using (var aes = CreateAes())
            using (var dec = aes.CreateDecryptor(key, initializationVector))
            using (var inFS = File.OpenRead(path))
            using (var outFS = File.OpenWrite(path + ".temp"))
            using (var cs = new CryptoStream(inFS, dec, CryptoStreamMode.Read))
            {
                progress.FineMax = inFS.Length;
                return TransformFile(progress, cs, outFS);
            }
        }

        protected Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            return aes;
        }

        // https://stackoverflow.com/a/32437759/14357934
        private Status TransformFile(BackgroundProgress progress, Stream readFrom, Stream writeTo)
        {
            const int bufferSize = 4096;
            progress.FineProgress = 0;
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
                        return new Status(-1, d["File read timed out."]);
                    if (remainingBytes == 0) break;
                    try
                    { bytesRead = readFrom.Read(buffer, position, remainingBytes); }
                    catch (Exception)
                    { return new Status(-2, d["Error occured while reading file."]); }
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
                catch (Exception)
                { return new Status(-3, d["Error occured while writing file."]); }
                progress.FineProgress += remainingBytes;
                // int progress = (int)(((double)bytesProcessed / inStreamSize) * 100.0);
                if (progress.CancellationPending)
                    return new Status(1);
                // Jeżeli bez problemów doszliśmy do końca pliku (EOF)
                // Przerywamy pętlę while (true).
                if (bytesRead == 0)
                    return new Status(0);
            }
        }

        private void FileTransformationCleanup(BackgroundProgress progress,
            Status status, string path)
        {
            if (progress.CancellationPending)
            {
                progress.Cancel = true;
                status.Code = 1;
            }
            var tempPath = path + ".temp";
            if (status.Code == 0)
            {
                File.Delete(path); // usuwamy stary plik
                // zmieniamy tymczasową nazwę nowego na nazwę starego
                File.Move(tempPath, path);
            }
            // jeżeli użytkownik anulował lub wystąpił błąd, usuwamy nowy plik
            else if (File.Exists(tempPath))
                File.Delete(tempPath);
            progress.Result = status;
        }

        public void EncryptDirectory(BackgroundProgress progress,
            string path, byte[] key, byte[] initializationVector) =>
            TransformDirectory(progress, path, key, initializationVector, EncryptSingleFile);

        public void DecryptDirectory(BackgroundProgress progress,
            string path, byte[] key, byte[] initializationVector) =>
            TransformDirectory(progress, path, key, initializationVector, DecryptSingleFile);

        private delegate Status FileTransformation(BackgroundProgress progress,
            string path, byte[] key, byte[] initializationVector);

        private void TransformDirectory(BackgroundProgress progress,
            string path, byte[] key, byte[] initializationVector,
            FileTransformation transformation)
        {
            var files = Directory.GetFiles(path);
            foreach (var f in files)
                Debug.WriteLine(f);
            progress.CoarseMax = files.Length - 1;
            progress.CoarseProgress = 0;
            var status = new Status(0);
            for (int i = 0; i < files.Length; ++i)
            {
                if (progress.CancellationPending) // nie ustawiamy tu status.Code = 1, bo zostanie ustawione w DirectoryTransformationCleanup
                    goto DIRECTORY_CLEANUP;
                status = transformation(progress, files[i], key, initializationVector);
                if (status.Code != 0)
                    goto DIRECTORY_CLEANUP;
                progress.CoarseProgress += 1;
            }
        DIRECTORY_CLEANUP:
            DirectoryTransformationCleanup(progress, status, files);
        }

        private void DirectoryTransformationCleanup(BackgroundProgress progress,
            Status status, string[] files)
        {
            if (progress.CancellationPending)
            {
                progress.Cancel = true;
                status.Code = 1;
            }
            if (status.Code == 0)
            {
                foreach (var f in files)
                {
                    var temp = f + ".temp";
                    File.Delete(f); // usuwamy stary plik
                    // zmieniamy tymczasową nazwę nowego na nazwę starego
                    File.Move(temp, f);
                }
            }
            // jeżeli użytkownik anulował lub wystąpił błąd, usuwamy nowe pliki
            else
                foreach (var f in files)
                {
                    var temp = f + ".temp";
                    if (File.Exists(temp))
                        File.Delete(temp);
                }
            progress.Result = status;
        }
    }
}