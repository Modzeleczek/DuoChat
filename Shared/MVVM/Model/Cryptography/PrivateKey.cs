using Shared.MVVM.View.Localization;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public class PrivateKey
    {
#if x64
        private const string DLL_PATH = "Prime_x64.dll";
#else
        private const string DLL_PATH = "Prime_x86.dll";
#endif
        private const CallingConvention CONVENTION = CallingConvention.Cdecl;

        // unsigned bit-endian - P[0] to najbardziej znaczący bajt liczby
        private byte[] _p;
        private byte[] _q;

        private PrivateKey(byte[] p, byte[] q)
        {
            _p = p;
            _q = q;
        }

        public static void Random(ProgressReporter reporter)
        {
            reporter.Result = RandomInner(reporter);
        }

        private static Status RandomInner(ProgressReporter reporter)
        {
            reporter.FineMax = 2;
            reporter.FineProgress = 0;
            using (var rng = RandomNumberGenerator.Create())
            {
                // generujemy 2 losowe liczby z zakresu <0, 2^(8*128)-1>
                BigInteger p = GenerateRandom(128, false, rng);
                BigInteger q = GenerateRandom(128, false, rng);
                /* zapewniamy, że obie wylosowane liczby są większe lub równe 2^(8*64) = 2^512;
                 * wówczas ich iloczyn zawsze będzie większy lub równy 2^512 * 2^512 = 2^1024;
                 * włączamy najmniej znaczący bit bajtu o indeksie 64 (licząc od 0) */
                var one = new BigInteger(1);
                p |= (one << (64 * 8));
                q |= (one << (64 * 8));

                p = FirstProbablePrimeGreaterThan(p);
                reporter.FineProgress = 1;
                if (reporter.CancellationPending) return new Status(1);

                q = FirstProbablePrimeGreaterThan(q);
                reporter.FineProgress = 2;
                if (reporter.CancellationPending) return new Status(1);

                return new Status(0, null, new PrivateKey(
                    ToUnsignedBigEndian(p), ToUnsignedBigEndian(q)));
            }
        }

        public static bool TryParse(string text, out PrivateKey ret)
        {
            ret = null;
            if (text == null) return false;
            var split = text.Split(';');
            if (split.Length != 2) return false;
            try
            {
                byte[] p = Convert.FromBase64String(split[0]);
                if (!IsProbablePrime(p)) return false;
                byte[] q = Convert.FromBase64String(split[1]);
                if (!IsProbablePrime(q)) return false;
                ret = new PrivateKey(p, q);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static void TryParse(ProgressReporter reporter, string text)
        {
            reporter.Result = TryParseInner(reporter, text);
        }

        private static Status TryParseInner(ProgressReporter reporter, string text)
        {
            var d = Translator.Instance;
            reporter.FineMax = 2;
            reporter.FineProgress = 0;

            if (text == null)
                return new Status(-1, d["String is null."]);

            var split = text.Split(';');
            if (split.Length != 2)
                return new Status(-2, d["String does not consist of two parts separated with semicolon."]);

            byte[] p = null;
            try
            { p = Convert.FromBase64String(split[0]); }
            catch (FormatException)
            { return new Status(-3, d["First number (p) is not valid Base64 string."]); }

            byte[] q = null;
            try
            { q = Convert.FromBase64String(split[1]); }
            catch (FormatException)
            { return new Status(-4, d["Second number (q) is not valid Base64 string."]); }

            if (!IsProbablePrime(p)) return new Status(-5, d["First number (p) is not prime."]);
            reporter.FineProgress = 1;
            if (reporter.CancellationPending) return new Status(1);

            if (!IsProbablePrime(q)) return new Status(-6, d["Second number (q) is not prime."]);
            reporter.FineProgress = 2;
            if (reporter.CancellationPending) return new Status(1);

            return new Status(0, null, new PrivateKey(p, q));
        }

        private static bool IsProbablePrime(byte[] number)
        {
            int isPrime = 0; // 0 - na pewno złożona; 1 - prawdopodobnie pierwsza
            unsafe
            {
                fixed (byte* numPtr = number)
                    if (is_probable_prime(numPtr, (uint)number.Length, 1, &isPrime) < 0)
                        throw new ExternalException("Cannot determine if number is probable prime.");
            }
            return isPrime == 1;
        }

        public static PrivateKey Parse(string text)
        {
            if (!TryParse(text, out PrivateKey value))
                throw new FormatException("Invalid PrivateKey format.");
            return value;
        }

        public override string ToString()
        {
            return $"{Convert.ToBase64String(_p)};{Convert.ToBase64String(_q)}";
        }

        public static PrivateKey FromBytes(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var lengthBuffer = new byte[2];

                ms.Read(lengthBuffer, 0, 2);
                var pLength = BitConverter.ToUInt16(lengthBuffer, 0);
                var p = new byte[pLength];
                ms.Read(p, 0, pLength);

                ms.Read(lengthBuffer, 0, 2);
                var qLength = BitConverter.ToUInt16(lengthBuffer, 0);
                var q = new byte[qLength];
                ms.Read(q, 0, qLength);

                return new PrivateKey(p, q);
            }
        }

        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(BitConverter.GetBytes((ushort)_p.Length), 0, 2);
                ms.Write(_p, 0, _p.Length);

                ms.Write(BitConverter.GetBytes((ushort)_q.Length), 0, 2);
                ms.Write(_q, 0, _q.Length);

                return ms.ToArray();
            }
        }

        private static BigInteger FirstProbablePrimeGreaterThan(BigInteger min)
        {
            /* test pierwszości Millera-Rabina stwierdza, że liczba jest złożona lub prawdopodobnie (ale nie na pewno) pierwsza; trzeci parametr mpz_probable_prime_p równy np. 100 oznacza, że chcemy, aby prawdopodobieństwo, że liczba złożona jest nazwana pierwszą, wynosiło 2^(-100) */
            byte[] bytes;
            unsafe
            {
                byte[] minBytes = min.ToByteArray();
                byte* primePtr;
                uint length;
                // blokujemy pozycję tablicy minBytes w pamięci na czas wywołania funkcji, aby w tym czasie GC nie przesunął tablicy w inne miejsce, przez co wskaźnik minPtr przestałby poprawnie wskazywać na tablicę
                fixed (byte* minPtr = minBytes)
                    if (first_probable_prime_greater_than(minPtr, (uint)minBytes.Length, -1,
                        &primePtr, &length) < 0)
                        throw new ExternalException("Cannot generate random probable prime number.");
                // jeżeli najbardziej znaczący bit ostatniego bajtu liczby jest 1, to dodanie ostatniego bajtu równego 0, którego najbardziej znaczący bit jest 0, powoduje, że BigInteger jest nieujemny
                bytes = new byte[length + 1];
                // kopiujemy bajty liczby z pamięci niezarządzanej do zarządzanej
                // length jest zawsze dużo mniejsze niż 2^31-1 (int.MaxValue)
                Marshal.Copy((IntPtr)primePtr, bytes, 0, (int)length);
                free_unmanaged(primePtr);
            }
            return new BigInteger(bytes);
        }

        private static BigInteger GenerateRandom(int octetCount, bool sign, RandomNumberGenerator rng)
        {
            byte[] octets = new byte[octetCount];
            rng.GetBytes(octets);
            // losowe są bity o indeksach od 0 do byteCount*8-2, bo ostatni ustawiamy w zależności od pożądanego znaku liczby
            if (sign == false) // nieujemna
                octets[octets.Length - 1] &= 0b0111_1111;
            else // ujemna
                octets[octets.Length - 1] |= 0b1000_0000;
            return new BigInteger(octets);
        }

        private static byte[] ToUnsignedBigEndian(BigInteger bi)
        {
            // wygenerowana liczba nie może być ujemna
            /* if (bi.Sign == -1)
                throw new Exception($"Generated prime number cannot be negative."); */
            byte[] bytes = bi.ToByteArray();
            bytes[bytes.Length - 1] &= 0b0111_1111; // tylko zerujemy bit znaku, ale nie wyznaczamy wartości bezwzględnej, jeżeli bi ujemne, bo zakładamy, że jest dodatnie
            // jeżeli najbardziej znaczący bajt przechowuje tylko bit znaku i między nim a właściwą liczbą same nieznaczące zera
            if (bytes[bytes.Length - 1] == 0b0000_0000)
                Array.Resize(ref bytes, bytes.Length - 1);
            Reverse(bytes);
            return bytes;
        }

        private static void Reverse(byte[] array)
        {
            int len = array.Length;
            for (int i = 0; i < len / 2; ++i)
            {
                byte temp = array[i];
                array[i] = array[len - 1 - i];
                array[len - 1 - i] = temp;
            }
        }

        private BigInteger ToBigInteger(byte[] number)
        {
            // bytes[0] to najbardziej znaczący bajt liczby
            // jeżeli najbardziej znaczący bajt ma najbardziej znaczący bit równy 1
            byte[] clone = null;
            if ((number[0] & 0b1000_0000) != 0)
            {
                // dodajemy nowy bajt przechowujący najbardziej znaczący bit znaku równy 0 i 7 bitów równych 0
                clone = new byte[number.Length + 1];
                number.CopyTo(clone, 1);
                number[0] = 0b0000_0000;
            }
            else
            {
                clone = new byte[number.Length];
                number.CopyTo(clone, 0);
            }
            // odwracamy kolejność bajtów
            Reverse(clone);
            return new BigInteger(clone);
        }

        public void ImportTo(RSA rsa)
        {
            BigInteger mod = ToBigInteger(_p) * ToBigInteger(_q);
            var par = new RSAParameters();
            par.P = _p;
            par.Q = _q;
            par.Exponent = PublicKey.PUBLIC_EXPONENT;
            par.Modulus = ToUnsignedBigEndian(mod);
            rsa.ImportParameters(par);
        }

        [DllImport(DLL_PATH, CallingConvention = CONVENTION)]
        unsafe private static extern
            int first_probable_prime_greater_than
            (byte* min_bytes, uint min_length, sbyte endian,
            byte** prime_bytes, uint* prime_length);

        [DllImport(DLL_PATH, CallingConvention = CONVENTION)]
        unsafe private static extern
            void free_unmanaged(void* array);

        [DllImport(DLL_PATH, CallingConvention = CONVENTION)]
        // endian: -1 - little; 1 - big
        unsafe private static extern
            int is_probable_prime(byte* bytes, uint length, sbyte endian, int* result);
    }
}
