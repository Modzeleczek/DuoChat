using Shared.MVVM.Core;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System;
using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public class PrivateKey
    {
        #region Fields
        private const string DLL_PATH = "Prime.dll";
        private const CallingConvention CONVENTION = CallingConvention.Cdecl;

        // unsigned big-endian - P[0] to najbardziej znaczący bajt liczby
        private readonly RSAParameters _parameters;
        #endregion

        private PrivateKey(byte[] pULE, byte[] qULE)
        {
            /* ULE znaczy, że liczby są w postaci unsigned little-endian gotowej
            do przekazania do konstruktora BigInteger. */

            /* Założenia, kiedy chcemy zaimportować klucz prywatny do implementacji RSA RSACng.
            https://en.wikipedia.org/wiki/RSA_(cryptosystem)#Using_the_Chinese_remainder_algorithm
            P, Q - liczby pierwsze
            E = 65537
            D = E^(-1) (mod (p - 1) * (q - 1))
            DP = D (mod Q - 1)
            DQ = D (mod P - 1)
            InverseQ = Q^(-1) (mod P)

            https://github.com/dotnet/runtime/issues/30914#issuecomment-533545099
            https://github.com/dotnet/runtime/pull/37668/files
            Modulus: No leading 0
            Exponent: No leading 0
            D: Same Length as Modulus, leading zeros as necessary to make that happen.
            P: Length is ((Modulus.Length + 1) / 2), leading zeros as necessary.
            Q, DP, DQ, InverseQ: Same length as P, leading zeros as necessary. */
            var p = ULEToBI(pULE);
            var q = ULEToBI(qULE);
            var m = p * q;
            var e = ULEToBI(PublicKey.PUBLIC_EXPONENT);
            var d = ModularInverse(e, (p - 1) * (q - 1));
            var dp = d % (q - 1);
            var dq = d % (p - 1);
            var invQ = ModularInverse(q, p);

            int modulusLength = m.GetByteCount();
            // Połowa modułu zaokrąglona w górę.
            int halfModulusLength = (modulusLength + 1) / 2;
            _parameters = new RSAParameters
            {
                Modulus = BIToPaddedUBE(m, modulusLength),
                Exponent = BIToPaddedUBE(e, e.GetByteCount()),
                D = BIToPaddedUBE(d, modulusLength),
                P = BIToPaddedUBE(p, halfModulusLength),
                Q = BIToPaddedUBE(q, halfModulusLength),
                DP = BIToPaddedUBE(dp, halfModulusLength),
                DQ = BIToPaddedUBE(dq, halfModulusLength),
                InverseQ = BIToPaddedUBE(invQ, halfModulusLength)
            };
        }

        #region Format conversions
        private static byte[] BIToULE(BigInteger bi)
        {
            return bi.ToByteArray(true, false);
        }

        private BigInteger ULEToBI(byte[] ule)
        {
            return new BigInteger(new ReadOnlySpan<byte>(ule), true, false);
        }

        private static byte[] BIToPaddedUBE(BigInteger bi, int byteCount)
        {
            byte[] biBytes = bi.ToByteArray(true, false);
            if (biBytes.Length < byteCount)
                Array.Resize(ref biBytes, byteCount);
            Array.Reverse(biBytes);
            return biBytes;
        }

        private byte[] UBEToMinimalULE(byte[] ube)
        {
            return new BigInteger(new ReadOnlySpan<byte>(ube), true, true)
                .ToByteArray(true, false);
        }

        private BigInteger UBEToBI(byte[] ube)
        {
            return new BigInteger(new ReadOnlySpan<byte>(ube), true, true);
        }
        #endregion

        private BigInteger ModularInverse(BigInteger a, BigInteger m)
        {
            /* https://www.geeksforgeeks.org/multiplicative-inverse-under-modulo-m/
            złożoność czasowa log(m); zwraca rozwiązanie x równania a*x = 1 (mod m)
            odwrotność liczby a w mnożeniu modulo m istnieje tylko, gdy a i m są względnie pierwsze
            (NWD(a, m) = 1)
            przykład wykonania rozszerzonego algorytmu Euklidesa dla 21*x = 1 (mod 37)
            37 = 1*21 + 16
            21 = 1*16 + 5
            16 = 3*5 + 1
            5 = 5*1 + 0
            1 = 16 - 3*5 = 16 - 3*(21 - 1*16) = 16 - 3*21 + 3*16 = -3*21 + 4*16 = -3*21 + 4*(37 - 1*21) =
            -7*21 + 4*37 = -7*21 (mod 37) = 30*21 (mod 37) */
            BigInteger m0 = m; // zapisujemy oryginalne m, aby przesunąć o nie x, jeżeli wyjdzie ujemne
            BigInteger y = 0, x = 1;
            if (m == 1) return 0; // pierścień Z1 zawiera tylko jedną liczbę - 0
            while (a > 1)
            {
                BigInteger q = a / m;
                BigInteger t = m;
                m = a % m;
                a = t;
                t = y;
                y = x - q * y;
                x = t;
            }
            if (x < 0) // przesuwamy x o oryginalne m
                x += m0;
            return x;
        }

        public static PrivateKey Random(int numberOfBits = 256 * 8,
            int enabledBitIndex = 64 * 8)
        {
            // Generujemy z mockowym ProgressReporterem.
            var doWorkEventArgs = new DoWorkEventArgs(null);
            var progressReporter = new ProgressReporter(doWorkEventArgs);
            Random(progressReporter, numberOfBits, enabledBitIndex);
            /* Jeżeli RandomInner wyrzuci wyjątek ArgumentException, to wyleci on
            do miejsca wywołania Random(int, int). Nie trzeba opakowywać go
            w obiekt typu Failure. */
            var result = doWorkEventArgs.Result;
            // Cancellation jest niemożliwe, bo nie ma interakcji ze strony użytkownika.
            return (PrivateKey)((Success)result!).Data!;
        }

        public static void Random(ProgressReporter reporter,
            int numberOfBits = 256 * 8, int enabledBitIndex = 64 * 8)
        {
            reporter.SetResult(RandomInner(reporter, numberOfBits, enabledBitIndex));
        }

        private static Result RandomInner(ProgressReporter reporter,
            int numberOfBits = 256 * 8, int enabledBitIndex = 64 * 8)
        {
            /* enabledBitIndex gwarantuje, że wartość wygenerowanego
            klucza prywatnego będzie większa lub równa 2^enabledBitIndex. */
            if (enabledBitIndex > numberOfBits - 1)
                throw new ArgumentException("enabledBitIndex must be less than numberOfBits",
                    nameof(enabledBitIndex));

            reporter.FineMax = 2;
            reporter.FineProgress = 0;
            using (var rng = RandomNumberGenerator.Create())
            {
                // generujemy 2 losowe liczby z zakresu <0, 2^(8*128)-1>
                BigInteger p = GenerateRandom(numberOfBits / 2, false, rng);
                BigInteger q = GenerateRandom(numberOfBits / 2, false, rng);
                /* Zapewniamy, że losowo wybrana liczba spośród losowych liczb p i q,
                jest większa lub równa 2^(8*64) = 2^512. Wówczas iloczyn p*q (q >= 2)
                zawsze jest większy lub równy 2^512. Włączamy najmniej znaczący bit
                bajtu o indeksie 64 (licząc od 0). */
                var one = new BigInteger(1);
                var randomByte = new byte[1];
                rng.GetBytes(randomByte);
                // najmniej znaczący bit jest 0
                if ((randomByte[0] & 0b0000_0001) == 0)
                    p |= (one << enabledBitIndex);
                else // najmniej znaczący bit jest 1
                    q |= (one << enabledBitIndex);

                p = FirstProbablePrimeGreaterOrEqual(p);
                reporter.FineProgress = 1;
                if (reporter.CancellationPending)
                    return new Cancellation();

                q = FirstProbablePrimeGreaterOrEqual(q);
                reporter.FineProgress = 2;
                if (reporter.CancellationPending)
                    return new Cancellation();

                /* TODO: sprawdzać, czy wygenerowane p i q są mniejsze
                lub równe 2^numberOfBits - 1. */
                return new Success(new PrivateKey(BIToULE(p), BIToULE(q)));
            }
        }

        public static bool TryParse(string text, out PrivateKey? ret)
        {
            ret = null;
            try
            {
                ret = Parse(text);
                return true;
            }
            catch (Error)
            {
                return false;
            }
        }

        public static void Parse(ProgressReporter reporter, string text)
        {
            reporter.SetResult(ParseInner(reporter, text));
        }

        private static Result ParseInner(ProgressReporter reporter, string text)
        {
            reporter.FineMax = 2;
            reporter.FineProgress = 0;

            if (text == null)
                return new Failure("|String is null.|");

            var split = text.Split(';');
            if (split.Length != 2)
                return new Failure(
                    "|String does not consist of two parts separated with semicolon.|");

            // np. gdy text == ";"
            if (string.IsNullOrEmpty(split[0]))
                return new Failure("|First| |number| (p) |is empty.|");

            if (string.IsNullOrEmpty(split[1]))
                return new Failure("|Second| |number| (q) |is empty.|");

            byte[] p;
            try { p = Convert.FromBase64String(split[0]); }
            catch (FormatException e)
            {
                return new Failure(e, "|First| |number| (p) " +
                    "|is not valid Base64 string.|");
            }

            byte[] q;
            try { q = Convert.FromBase64String(split[1]); }
            catch (FormatException e)
            {
                return new Failure(e, "|Second| |number (q) " +
                    "|is not valid Base64 string.|");
            }

            if (!IsProbablePrime(p))
                return new Failure("|First| |number| (p) |is not prime.|");
            reporter.FineProgress = 1;
            if (reporter.CancellationPending)
                return new Cancellation();

            if (!IsProbablePrime(q))
                return new Failure("|Second| |number| (q) |is not prime.|");
            reporter.FineProgress = 2;
            if (reporter.CancellationPending)
                return new Cancellation();

            /* TODO: sprawdzać, czy sparsowane p i q są mniejsze
            lub równe 2^(8*128) - 1. */
            return new Success(new PrivateKey(p, q));
        }

        private static bool IsProbablePrime(byte[] number)
        {
            int isPrime = 0; // 0 - na pewno złożona; 1 - prawdopodobnie pierwsza
            unsafe
            {
                fixed (byte* numPtr = number)
                    // endian = 1 - big; -1 - little
                    if (is_probable_prime(numPtr, (uint)number.Length, -1, &isPrime) < 0)
                        throw new ExternalException("Cannot determine if number is probable prime.");
            }
            return isPrime == 1;
        }

        public static PrivateKey Parse(string text)
        {
            var doWorkEventArgs = new DoWorkEventArgs(null);
            var progressReporter = new ProgressReporter(doWorkEventArgs);
            Parse(progressReporter, text);
            var result = doWorkEventArgs.Result;
            // Cancellation jest niemożliwe, bo nie ma interakcji ze strony użytkownika.
            if (result is Failure failure)
                throw failure.Reason;
            return (PrivateKey)((Success)result!).Data!;
        }

        public override string ToString()
        {
            byte[] p = UBEToMinimalULE(_parameters.P!);
            byte[] q = UBEToMinimalULE(_parameters.Q!);

            return $"{Convert.ToBase64String(p)};{Convert.ToBase64String(q)}";
        }

        public static PrivateKey FromBytes(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var lengthBuffer = new byte[sizeof(ushort)];

                ms.Read(lengthBuffer, 0, sizeof(ushort));
                ushort pLength = BitConverter.ToUInt16(lengthBuffer, 0);
                byte[] p = new byte[pLength];
                ms.Read(p, 0, pLength);

                ms.Read(lengthBuffer, 0, sizeof(ushort));
                ushort qLength = BitConverter.ToUInt16(lengthBuffer, 0);
                byte[] q = new byte[qLength];
                ms.Read(q, 0, qLength);

                return new PrivateKey(p, q);
            }
        }

        public byte[] ToBytes()
        {
            // ToByteArray zapisuje BigInteger na minimalnej potrzebnej liczbie bajtów.
            byte[] p = UBEToMinimalULE(_parameters.P!);
            byte[] q = UBEToMinimalULE(_parameters.Q!);

            using (var ms = new MemoryStream())
            {
                /* ToBytes używamy tylko do konwersji klucza prywatnego do przechowywania w lokalnej bazie.
                Nie przesyłamy go do innego hosta, więc nie trzeba dbać o sieciową kolejność bajtów. */
                ms.Write(BitConverter.GetBytes((ushort)p.Length), 0, sizeof(ushort));
                ms.Write(p, 0, p.Length);

                ms.Write(BitConverter.GetBytes((ushort)q.Length), 0, sizeof(ushort));
                ms.Write(q, 0, q.Length);

                return ms.ToArray();
            }
        }

        private static BigInteger FirstProbablePrimeGreaterOrEqual(BigInteger min)
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
                    // endian = 1 - big; -1 - little; BigInteger przechowuje bajty w kolejności little-endian
                    if (first_probable_prime_greater_or_equal(minPtr, (uint)minBytes.Length, -1,
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

        private static BigInteger GenerateRandom(int bitCount, bool sign,
            RandomNumberGenerator rng)
        {
            // Jeżeli sign == true, to generujemy liczbę ujemną.
            if (bitCount <= 0)
                throw new ArgumentException("bitCount must be greater than 0",
                    nameof(bitCount));

            /* bitCount to liczba bitów liczby łącznie z bitem znaku.
            Losowo generujemy tylko bitCount-1 bitów (bez bitu znaku). */
            int byteCount = bitCount / 8; // >> 3
            int remainder = bitCount % 8; // & 7
            if (remainder > 0)
                byteCount += 1;
            // Alternatywa
            // int byteCount = bitCount / 8 + ((bitCount % 8) + 7) / 8;

            byte[] bytes = new byte[byteCount];
            rng.GetBytes(bytes);

            int lastIndex = bytes.Length - 1;
            /* Wyłączamy z ostatniego bajtu niepotrzebnie wygenerowane bity,
            np. dla bitCount = 6, potrzebujemy tylko 6 bitów, a wygenerowaliśmy
            cały bajt, więc wyłączamy jego 2 najstarsze bity i 1 bit znaku.
            bitCount    bytes[lastIndex] &  1111_1111 >>
            0           -                   -           liczba nie istnieje
            1           z000_0000           8           tylko bit znaku
            2           z000_0001           7
            3           z000_0011           6
            4           z000_0111           5
            5           z000_1111           4
            6           z001_1111           3
            7           z011_1111           2
            8           z111_1111           1
            9           z000_0000 1111_1111 8
            ... */
            bytes[lastIndex] &= (byte)(0b1111_1111 >> (8 - ((bitCount - 1) % 8)));

            /* Wyłączamy najbardziej znaczący bit najbardziej
            znaczącego bajtu (bit znaku). */
            bytes[lastIndex] &= 0b0111_1111;

            /* Mamy liczbę bez znaku (unsigned) o wartości
            z przedziału <0, 2^bitCount - 1>. */
            var ret = new BigInteger(bytes);
            if (!sign) // Nieujemna - zwracamy bez zmian.
                return ret;
            else // Ujemna - wykonujemy negację i zwiększenie o 1.
                return -ret;
        }

        public void ImportTo(RSA rsa)
        {
            rsa.ImportParameters(_parameters);
        }

        public PublicKey ToPublicKey()
        {
            BigInteger p = UBEToBI(_parameters.P!);
            BigInteger q = UBEToBI(_parameters.Q!);
            BigInteger mod = p * q;
            return new PublicKey(BIToPaddedUBE(mod, mod.GetByteCount()));
        }

        [DllImport(DLL_PATH, CallingConvention = CONVENTION)]
        unsafe private static extern
            int first_probable_prime_greater_or_equal
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
