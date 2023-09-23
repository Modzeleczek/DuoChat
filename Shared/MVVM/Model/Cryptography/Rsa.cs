using Shared.MVVM.Core;
using System;
using System.Security.Cryptography;

namespace Shared.MVVM.Model.Cryptography
{
    public static class Rsa
    {
        /* Zapewnienie poufności:
        Za pomocą System.Security.Cryptography.RSA.Encrypt można
        szyfrować tylko kluczem publicznym (w RSAParameters trzeba zapisać
        Modulus, Exponent). Poprzez Decrypt można odszyfrowywać tylko
        kluczem prywatnym (w RSAParameters: Modulus, Exponent, P, Q).

        Zapewnienie integralności:
        Do generowania haszu (skrótu) wiadomości służy metoda SignData.
        VerifyData służy do weryfikacji haszu.

        Uwierzytelnianie (autentykacja, zapewnienie, że nadawca jest
        tym, za kogo się podaje):
        Metoda SignData liczy hasz i podpisuje (szyfruje) go kluczem prywatnym
        nadawcy. Odbiorca weryfikuje podpis kluczem publicznym nadawcy metodą
        VerifyData. */

        private static readonly RSAEncryptionPadding ENCRYPTION_PADDING = RSAEncryptionPadding.OaepSHA256;
        private static readonly HashAlgorithmName SIGNATURE_HASH_ALGORITHM = HashAlgorithmName.SHA256;
        private static readonly RSASignaturePadding SIGNATURE_PADDING = RSASignaturePadding.Pkcs1;

        public static byte[] Sign(PrivateKey privateKey, byte[] data)
        {
            try
            {
                using (var rsa = CreateRsa())
                {
                    privateKey.ImportTo(rsa);
                    // Otrzymany podpis zawsze ma długość 256 bajtów.
                    return rsa.SignData(data, SIGNATURE_HASH_ALGORITHM, SIGNATURE_PADDING);
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |RSA signing|.");
            }
        }

        private static RSACng CreateRsa() => new RSACng();

        public static bool Verify(PublicKey publicKey, byte[] data, byte[] signature)
        {
            try
            {
                using (var rsa = CreateRsa())
                {
                    publicKey.ImportTo(rsa);
                    /* Jeżeli dane zostały zmodyfikowane, a sygnatura nie, to VerifyData zwróci false.
                    Jeżeli sygnatura została "ręcznie" zmodyfikowana i nie pasuje do algorytmu
                    haszującego i metody paddingu, to VerifyData wyrzuci wyjątek (ciężko go spowodować). */
                    return rsa.VerifyData(data, signature, SIGNATURE_HASH_ALGORITHM, SIGNATURE_PADDING);
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |verifying RSA signature|.");
            }
        }

        public static byte[] Encrypt(PublicKey key, byte[] plain)
        {
            try
            {
                using (var rsa = CreateRsa())
                {
                    key.ImportTo(rsa);
                    return rsa.Encrypt(plain, ENCRYPTION_PADDING);
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |RSA encrypting.|");
            }
        }

        public static byte[] Decrypt(PrivateKey key, byte[] cipher)
        {
            return Decrypt(key, cipher, 0, cipher.Length);
        }

        public static byte[] Decrypt(PrivateKey key, byte[] cipher, int startIndex, int count)
        {
            var slice = new byte[count];
            Buffer.BlockCopy(cipher, startIndex, slice, 0, count);

            try
            {
                using (var rsa = CreateRsa())
                {
                    key.ImportTo(rsa);
                    return rsa.Decrypt(slice, ENCRYPTION_PADDING);
                }
            }
            catch (Exception e)
            {
                throw new Error(e, "|Error occured while| |RSA decrypting.|");
            }
        }
    }
}
