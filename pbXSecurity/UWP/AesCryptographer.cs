﻿#if WINDOWS_UWP

using System;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

using pbXNet;

namespace pbXSecurity
{
    public partial class AesCryptographer: ICryptographer
    {
        public byte[] GenerateKey(byte[] pwd, byte[] salt, int length = 32)
        {
            const int iterations = 10000;

            KeyDerivationAlgorithmProvider objKdfProv = KeyDerivationAlgorithmProvider.OpenAlgorithm(KeyDerivationAlgorithmNames.Pbkdf2Sha1);
            IBuffer buffPwd = CryptographicBuffer.CreateFromByteArray(pwd);
            IBuffer buffSalt = CryptographicBuffer.CreateFromByteArray(salt);
            KeyDerivationParameters pbkdf2Params = KeyDerivationParameters.BuildForPbkdf2(buffSalt, iterations);
            CryptographicKey keyOriginal = objKdfProv.CreateKey(buffPwd);
            IBuffer buffKey = CryptographicEngine.DeriveKeyMaterial(keyOriginal, pbkdf2Params, (uint)length);
            return IBufferToByteArray(buffKey);
        }

        public byte[] GenerateIV(int length = 16)
        {
            IBuffer buff = CryptographicBuffer.GenerateRandom((uint)length);
            return IBufferToByteArray(buff);
        }

        //

        public byte[] Encrypt(byte[] msg, byte[] key, byte[] iv)
        {
            // algoritm
            SymmetricKeyAlgorithmProvider objAlg = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);

            // prepare
            IBuffer buffKey = CryptographicBuffer.CreateFromByteArray(key);
            CryptographicKey ckey = objAlg.CreateSymmetricKey(buffKey);
            IBuffer buffMsg = CryptographicBuffer.CreateFromByteArray(msg);
            IBuffer buffIv = CryptographicBuffer.CreateFromByteArray(iv);

            // encrypt
            IBuffer buffMsgEncrypted = CryptographicEngine.Encrypt(ckey, buffMsg, buffIv);
            return IBufferToByteArray(buffMsgEncrypted);
        }

        public byte[] Decrypt(byte[] msg, byte[] key, byte[] iv)
        {
            // algoritm
            SymmetricKeyAlgorithmProvider objAlg = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);

            // prepare
            IBuffer buffKey = CryptographicBuffer.CreateFromByteArray(key);
            CryptographicKey ckey = objAlg.CreateSymmetricKey(buffKey);
            IBuffer buffMsg = CryptographicBuffer.CreateFromByteArray(msg);
            IBuffer buffIv = CryptographicBuffer.CreateFromByteArray(iv);

            // decrypt
            IBuffer buffMsgDecrypted = CryptographicEngine.Decrypt(ckey, buffMsg, buffIv);
            return IBufferToByteArray(buffMsgDecrypted);
        }


        //public static string IBufferToString(IBuffer buf)
        //{
        //    byte[] rawBytes = new byte[buf.Length];
        //    using (var reader = DataReader.FromBuffer(buf))
        //    {
        //        reader.ReadBytes(rawBytes);
        //    }
        //    var encoding = Encoding.UTF8;
        //    return encoding.GetString(rawBytes, 0, rawBytes.Length);
        //}

        private static byte[] IBufferToByteArray(IBuffer buf)
        {
            byte[] rawBytes = new byte[buf.Length];
            using (var reader = DataReader.FromBuffer(buf))
            {
                reader.ReadBytes(rawBytes);
            }
            return rawBytes;
        }
    }
}

#endif