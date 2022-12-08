using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Shared.Cryptography
{
    public static class RSA
    {
        public const int PUBLIC_EXPONENT = 65537; // często używana jako e liczba pierwsza Fermata 2^(2^4) + 1

        public static BigInteger OctetsToInt(byte[] octets)
        {
            // octets w kolejności big-endian
            BigInteger integer = 0;
            for (int i = 0; i < octets.Length; i++)
                integer = (integer << 8) | octets[i];
            return integer;
        }

        public static byte[] IntToOctets(BigInteger integer, int octetsLength)
        {
            byte[] octets = new byte[octetsLength];
            if (integer >= (new BigInteger(1) << (8 * octetsLength))) // 256^l = (2^8)^l = 2^(8*l)
                throw new ArgumentException("Integer is too large.");
            int i;
            for (i = octetsLength - 1; i >= 0; --i)
            {
                if (integer == 0)
                {
                    i += 1;
                    int j = 0;
                    for (; i < octetsLength; ++i)
                    {
                        octets[j++] = octets[i];
                        octets[i] = 0;
                    }
                    break;
                }
                octets[i] = (byte)(integer & 255);
                integer >>= 8;
            }
            return octets;
        }

        public struct Key<T>
        {
            public BigInteger Modulus { get; set; }
            public T Exponent { get; set; }

            public Key(BigInteger modulus, T exponent)
            {
                Modulus = modulus;
                Exponent = exponent;
            }
        }

        public struct KeyPair
        {
            public BigInteger Modulus { get; set; }
            public int PublicExponent { get; set; }
            public BigInteger PrivateExponent { get; set; }

            public KeyPair(BigInteger modulus, int publicExponent, BigInteger privateExponent)
            {
                Modulus = modulus;
                PublicExponent = publicExponent;
                PrivateExponent = privateExponent;
            }
        }

        public static KeyPair CreateKeyPair(BigInteger p, BigInteger q)
        {
            // p, q - duże liczby pierwsze o długości około 1024 bitów, aby ich iloczyn miał długość około 2048 bitów
            if (!IsProbablePrime(p)) throw new ArgumentException($"p = {p} is not prime.");
            if (!IsProbablePrime(q)) throw new ArgumentException($"q = {q} is not prime.");
            var n = p * q; // n = p * q – moduł
            // φ(n) = (p - 1) * (q - 1) = p * q - (p + q) + 1 = n - (p + q) + 1
            var fiN = n - (p + q) + 1;
            // e – liczba względnie pierwsza z φ(n)
            const int e = PUBLIC_EXPONENT;
            /* według https://crypto.stackexchange.com/questions/12255/in-rsa-why-is-it-important-to-choose-e-so-that-it-is-coprime-to-%CF%86n#comment216062_12256 można też sprawdzić, czy e jest względnie pierwsze jednocześnie z (p-1) i (q-1)
            if (!(GreatestCommonDivisor(e, p - 1) == 1 && GreatestCommonDivisor(e, q - 1) == 1)) */
            if (fiN % e == 0) // e jest liczbą pierwszą, więc jeżeli dzieli φ(n), to e i φ(n) nie są względnie pierwsze
                throw new ArgumentException(
                    $"φ(n) = (p-1)*(q-1) = {p - 1}*{q - 1} = {fiN} and e = {e} are not coprime.");
            // d – liczba wyznaczona tak, że zachodzi (e * d) mod φ(n) = 1 -> d = e^(-1) mod φ(n)
            var d = ModularInverse(e, fiN);
            return new KeyPair(n, e, d);
        }

        /* public static byte[] Encrypt(byte[] plain, BigInteger exponent, BigInteger modulus)
        {
            // długość szyfrowanego bloku jest równa plain.Length
            var integer = OctetsToInt(plain);
        } */

        private static BigInteger GreatestCommonDivisor(BigInteger a, BigInteger b)
        {
            // standardowy algorytm Euklidesa
            while (a != b)
                if (a > b) a -= b;
                else b -= a;
            return a;
        }

        private static BigInteger ModularInverse(BigInteger a, BigInteger m)
        {
            // https://stackoverflow.com/a/38198477/14357934
            /* https://www.geeksforgeeks.org/multiplicative-inverse-under-modulo-m/
            złożoność czasowa log(m); zwraca rozwiązanie x równania a*x = 1 (mod m)
            odwrotność liczby a w mnożeniu modulo m istnieje tylko, gdy a i m są względnie pierwsze (NWD(a, m) = 1)
            przykład wykonania rozszerzonego algorytmu Euklidesa dla 21*x = 1 (mod 37)
            37 = 1*21 + 16
            21 = 1*16 + 5
            16 = 3*5 + 1
            5 = 5*1 + 0
            1 = 16 - 3*5 = 16 - 3*(21 - 1*16) = 16 - 3*21 + 3*16 = -3*21 + 4*16 = -3*21 + 4*(37 - 1*21) = -7*21 + 4*37 = -7*21 (mod 37) = 30*21 (mod 37) */
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
            if (x < 0) x += m0; // przesuwamy x o oryginalne m
            return x;
        }

        private static BigInteger PowerModulo(BigInteger b, BigInteger e, BigInteger m)
        {
            // https://www.geeksforgeeks.org/modular-exponentiation-power-in-modular-arithmetic/; złożoność czasowa log(e)
            if (b >= m) b = b % m; // upewniamy się, że podstawa jest mniejsza od modułu
            if (b == 0)
            {
                if (e == 0) throw new ArgumentException("0^0 is undefined.");
                else return 0; // jeżeli podstawa = 0, a wykładnik != 0, to wynik też będzie 0 i nie trzeba potęgować
            }
            BigInteger res = 1; // inicjujemy wynik jako 1
            while (e > 0)
            {
                // jeżeli aktualny najmłodszy bit wykładnika jest 1, to mnożymy wynik przez aktualną podstawę
                if ((e & 1) != 0)
                    res = (res * b) % m;
                e = e >> 1; // e /= 2; przesuwamy się na na kolejny po aktualnym najmłodszym bicie wykładnika
                b = (b * b) % m; // zmieniamy podstawę na jej kwadrat modulo m
            }
            return res;
        }

        public static BigInteger FirstProbablePrimeGreaterThan(BigInteger min)
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

        public static bool IsProbablePrime(BigInteger number)
        {
            int result;
            byte[] numberBytes = number.ToByteArray();
            unsafe
            {
                fixed (byte* numberPtr = numberBytes)
                if (is_probable_prime(numberPtr, (uint)numberBytes.Length, -1,
                    &result) < 0)
                    throw new ExternalException($"Cannot check if {number} is prime.");
                return result == 1;
            }
        }

        public static BigInteger GenerateRandom(int byteCount, bool sign)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[byteCount];
                rng.GetBytes(bytes);
                // losowe są bity o indeksach od 0 do byteCount*8-2, bo ostatni ustawiamy w zależności od pożądanego znaku liczby
                if (sign == false) // nieujemna
                    bytes[bytes.Length - 1] &= 0b0111_1111;
                else // ujemna
                    bytes[bytes.Length - 1] |= 0b1000_0000;
                return new BigInteger(bytes);
            }
        }

        private const string DLL_PATH = "Prime.dll";
        private const CallingConvention CONVENTION = CallingConvention.Cdecl;
        [DllImport(DLL_PATH, CallingConvention = CONVENTION)]
        unsafe private static extern
            int first_probable_prime_greater_than
            (byte* min_bytes, uint min_length, sbyte endian,
            byte** prime_bytes, uint* prime_length);

        [DllImport(DLL_PATH, CallingConvention = CONVENTION)]
        unsafe private static extern
            void free_unmanaged(void* array);

        [DllImport(DLL_PATH, CallingConvention = CONVENTION)]
        unsafe private static extern
            int is_probable_prime(byte* bytes, uint length, sbyte endian, int* result);
    }
}
