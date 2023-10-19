using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Client.MVVM.Model
{
    public static class FileEncryptor
    {
        public static void EncryptFile(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector)
        {
            var result = EncryptSingleFile(reporter, path, key, initializationVector);
            FileTransformationCleanup(reporter, result, path);
        }

        public static void DecryptFile(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector)
        {
            var result = DecryptSingleFile(reporter, path, key, initializationVector);
            FileTransformationCleanup(reporter, result, path);
        }

        private static Result EncryptSingleFile(ProgressReporter reporter,
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

        private static Result DecryptSingleFile(ProgressReporter reporter,
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

        private static Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            return aes;
        }

        // https://stackoverflow.com/a/32437759/14357934
        private static Result TransformFile(ProgressReporter reporter, Stream readFrom, Stream writeTo)
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

        private static void FileTransformationCleanup(ProgressReporter reporter,
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
            reporter.SetResult(result);
        }

        private static string FileDeleteError(string path) =>
            $"|Error occured while| |deleting| |file| {path}.";

        private static string FileMoveError(string source, string destination) =>
            $"|Error occured while| |moving| |file| {source} to {destination}.";

        public static void EncryptDirectory(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector) =>
            TransformDirectory(reporter, path, key, initializationVector, EncryptSingleFile);

        public static void DecryptDirectory(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector) =>
            TransformDirectory(reporter, path, key, initializationVector, DecryptSingleFile);

        private delegate Result FileTransformation(ProgressReporter reporter,
            string path, byte[] key, byte[] initializationVector);

        private static void TransformDirectory(ProgressReporter reporter,
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

        private static void DirectoryTransformationCleanup(ProgressReporter reporter,
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

            reporter.SetResult(result);
        }

        private static string MergeErrorMessages(LinkedList<Error> errors)
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
