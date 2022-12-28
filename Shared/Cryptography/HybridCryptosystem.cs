using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Shared.Cryptography
{
    public class HybridCryptosystem : IDisposable
    {
        #if x64
        private const string DLL_PATH = "Prime_x64.dll";
        #else
        private const string DLL_PATH = "Prime_x86.dll";
        #endif
        private const CallingConvention CONVENTION = CallingConvention.Cdecl;

        // rozmiar w bajtach bloku, na które będzie dzielona wiadomość
        private const int BLOCK_SIZE = 16;
        // często używana jako e liczba pierwsza Fermata 2^(2^4) + 1
        private const int PUBLIC_EXPONENT = 65537;
        private System.Security.Cryptography.RSA _rsa = System.Security.Cryptography.RSA.Create();

        public HybridCryptosystem() { }

        public void Dispose()
        {
            _rsa.Dispose();
        }

        private byte[] GenerateRandom(int byteCount)
        {
            var bytes = new byte[byteCount];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return bytes;
        }

        private Aes CreateAes()
        {
            var aes = Aes.Create();
            /* PKCS7 jest metodą paddingu, czyli dopełniania tekstu jawnego do pełnych
             * bloków szyfru blokowego (u nas AES) przed jej zaszyfrowaniem. Zgodnie ze
             * specyfikacją w RFC5652, na końcu tekstu jawnego o długości l (w oktetach),
             * przy szyfrze o rozmiarze bloku równym k (w oktetach, metoda jest określona
             * tylko dla szyfrów o k < 256), dopisujemy k-(l mod k) oktetów o wartości 
             * k-(l mod k), np.
             * wiadomość DD DD DD DD | DD DD po dopełnieniu będzie ciągiem
             * DD DD DD DD | DD DD 02 02
             * wiadomość 12 34 56 78 | 90 12 34 56 po dopełnieniu będzie ciągiem
             * 12 34 56 78 | 90 12 34 56 | 04 04 04 04 */
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            return aes;
        }

        /* public byte[] Encrypt(byte[] plain)
        {
            byte[] key = GenerateRandom(128 / 8); // 128 b - rozmiar klucza AESa
            byte[] iv = GenerateRandom(128 / 8); // 128 b - rozmiar bloku AESa
            using (var ms = new MemoryStream())
            {
                _rsa.
                var ciphTxt = _rsa.Encrypt(buffer, padding);
            }
            using (var aes = CreateAes())
            using (var enc = aes.CreateEncryptor(key, iv))
            using (var inFS = File.OpenRead(path))
            using (var outFS = File.OpenWrite(path + ".temp"))
            using (var cs = new CryptoStream(outFS, enc, CryptoStreamMode.Write))
            {
                progress.FineMax = inFS.Length;
                return TransformFile(progress, inFS, cs);
            }

            var padding = RSAEncryptionPadding.OaepSHA256;
            byte[] buffer = new byte[blockSize];
            using (var rsa = System.Security.Cryptography.RSA.Create())
            using (var ms = new MemoryStream())
            {
                for (int i = 0; i < plain.Length; i += blockSize)
                {
                    Buffer.BlockCopy(plain, i, buffer, 0, blockSize);
                    var ciphTxt = _rsa.Encrypt(buffer, padding);
                    ms.Write(ciphTxt, 0, ciphTxt.Length);
                }
                int rem = plain.Length % blockSize;
                if (rem > 0)
                {
                    Array.Resize(ref buffer, rem);
                    Buffer.BlockCopy(plain, plain.Length - rem, buffer, 0, rem);
                    var ciphTxt = _rsa.Encrypt(buffer, padding);
                    ms.Write(ciphTxt, 0, ciphTxt.Length);
                }
                return ms.ToArray();
            }
        }

        private byte[] EncryptAesKeyIv()
        {

        } */

        /* public byte[] Decrypt(byte[] cipher, int blockSize)
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

            var padding = RSAEncryptionPadding.OaepSHA256;
            byte[] buffer = new byte[blockSize];
            using (var ms = new MemoryStream())
            {
                for (int i = 0; i < plain.Length; i += blockSize)
                {
                    Buffer.BlockCopy(plain, i, buffer, 0, blockSize);
                    var ciphTxt = _rsa.Encrypt(buffer, padding);
                    ms.Write(ciphTxt, 0, ciphTxt.Length);
                }
                int rem = plain.Length % blockSize;
                if (rem > 0)
                {
                    Array.Resize(ref buffer, rem);
                    Buffer.BlockCopy(plain, plain.Length - rem, buffer, 0, rem);
                    var ciphTxt = _rsa.Encrypt(buffer, padding);
                    ms.Write(ciphTxt, 0, ciphTxt.Length);
                }
                return ms.ToArray();
            }
        } */

        public struct PrivateKey
        {
            public BigInteger P { get; set; }
            public BigInteger Q { get; set; }

            public PrivateKey(BigInteger p, BigInteger q)
            {
                P = p;
                Q = q;
            }
        }

        public struct PublicKey
        {
            public BigInteger Modulus { get; set; }

            public PublicKey(BigInteger modulus)
            {
                Modulus = modulus;
            }
        }

        private BigInteger ModularInverse(BigInteger a, BigInteger m)
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

        private BigInteger FirstProbablePrimeGreaterThan(BigInteger min)
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

        private BigInteger GenerateRandom(int octetCount, bool sign, RandomNumberGenerator rng)
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

        public PrivateKey GeneratePrivateKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                // generujemy 2 liczby pierwsze z zakresu <0, 2^(8*128)-1>
                BigInteger p = GenerateRandom(128, false, rng);
                BigInteger q = GenerateRandom(128, false, rng);
                /* zapewniamy, że obie wylosowane liczby są większe lub równe 2^(8*64) = 2^512;
                 * wówczas ich iloczyn zawsze będzie większy lub równy 2^512 * 2^512 = 2^1024
                 * włączamy najmniej znaczący bit bajtu o indeksie 64 (licząc od 0) */
                var one = new BigInteger(1);
                p |= (one << (64 * 8));
                q |= (one << (64 * 8));
                p = FirstProbablePrimeGreaterThan(p);
                q = FirstProbablePrimeGreaterThan(q);
                return new PrivateKey(p, q);
            }
        }

        public void ImportRsaKey(PrivateKey key)
        {
            BigInteger mod = key.P * key.Q;
            var par = new RSAParameters();
            par.P = ToUnsignedBigEndian(key.P);
            par.Q = ToUnsignedBigEndian(key.Q);
            par.Exponent = ToUnsignedBigEndian(new BigInteger(PUBLIC_EXPONENT));
            par.Modulus = ToUnsignedBigEndian(mod);
            _rsa.ImportParameters(par);
        }

        public void ImportRsaKey(PublicKey key)
        {
            var par = new RSAParameters();
            par.Exponent = ToUnsignedBigEndian(new BigInteger(PUBLIC_EXPONENT));
            par.Modulus = ToUnsignedBigEndian(key.Modulus);
            _rsa.ImportParameters(par);
        }

        private byte[] ToUnsignedBigEndian(BigInteger bi)
        {
            // wygenerowana liczba nie może być ujemna
            /* if (bi.Sign == -1)
                throw new Exception($"Generated prime number cannot be negative."); */
            byte[] bytes = bi.ToByteArray();
            bytes[bytes.Length - 1] &= 0b0111_1111; // tylko zerujemy bit znaku, ale nie wyznaczamy wartości bezwzględnej, jeżeli bi ujemne, bo zakładamy, że jest dodatnie
            // jeżeli najbardziej znaczący bajt przechowuje tylko bit znaku i między nim a właściwą liczbą same nieznaczące zera
            if (bytes[bytes.Length - 1] == 0)
                Array.Resize(ref bytes, bytes.Length - 1);
            Reverse(bytes);
            return bytes;
        }

        private void Reverse(byte[] array)
        {
            int len = array.Length;
            for (int i = 0; i < len / 2; ++i)
            {
                byte temp = array[i];
                array[i] = array[len - 1 - i];
                array[len - 1 - i] = temp;
            }
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
        unsafe private static extern
            int is_probable_prime(byte* bytes, uint length, sbyte endian, int* result);
    }
}
