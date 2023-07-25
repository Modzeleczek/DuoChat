﻿using Shared.MVVM.Core;
using System;
using System.IO;
using System.Security.Cryptography;
using LibraryAes = System.Security.Cryptography.Aes;

namespace Shared.MVVM.Model.Cryptography
{
    public class Aes
    {
        private byte[] key, iv;

        public Aes(byte[] key, byte[] iv)
        {
            this.key = key;
            this.iv = iv;
        }

        public byte[] Encrypt(byte[] plain)
        {
            try
            {
                using (var aes = CreateAes())
                using (var encryptor = aes.CreateEncryptor(key, iv))
                    return AesTransform(encryptor, plain);
            }
            catch (Error e)
            {
                e.Prepend(AesTransformingError());
                throw;
            }
        }

        private string AesTransformingError() =>
            "|Error occured while| |AES transforming.|";

        private LibraryAes CreateAes()
        {
            var aes = LibraryAes.Create();
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            return aes;
        }

        private byte[] AesTransform(ICryptoTransform transform, byte[] bytes)
        {
            using (var output = new MemoryStream())
            using (var cs = new CryptoStream(output, transform, CryptoStreamMode.Write))
            {
                try
                {
                    cs.Write(bytes, 0, bytes.Length);
                    return output.GetBuffer();
                }
                catch (Exception e)
                {
                    throw new Error(e, "|Error occured while| " +
                        "|writing to AES transformation stream.|");
                }
            }
        }

        public byte[] Decrypt(byte[] cipher)
        {
            try
            {
                using (var aes = CreateAes())
                using (var decryptor = aes.CreateDecryptor(key, iv))
                    return AesTransform(decryptor, cipher);
            }
            catch (Error e)
            {
                e.Prepend(AesTransformingError());
                throw;
            }
        }
    }
}