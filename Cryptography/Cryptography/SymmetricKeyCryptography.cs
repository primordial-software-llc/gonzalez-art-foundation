using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Cryptography
{
    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/system.security.cryptography.aes(v=vs.110).aspx
    /// </summary>
    public class SymmetricKeyCryptography
    {
        public static string Encrypt(string data, string keyText, string initializationVectorText)
        {
            SHA256Managed crypt = new SHA256Managed();
            byte[] key = crypt.ComputeHash(Encoding.UTF8.GetBytes(keyText));

            byte[] ivFull = crypt.ComputeHash(Encoding.UTF8.GetBytes(initializationVectorText));
            byte[] iv = new byte[16];
            Array.Copy(ivFull, iv, iv.Length);

            byte[] encrypted = EncryptStringToBytes_Aes(data, key, iv);
            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string encrypted, string keyText, string initializationVectorText)
        {
            SHA256Managed crypt = new SHA256Managed();
            byte[] key = crypt.ComputeHash(Encoding.UTF8.GetBytes(keyText));

            byte[] ivFull = crypt.ComputeHash(Encoding.UTF8.GetBytes(initializationVectorText));
            byte[] initializationVector = new byte[16];
            Array.Copy(ivFull, initializationVector, initializationVector.Length);

            string plaintext;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = initializationVector;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encrypted)))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    plaintext = srDecrypt.ReadToEnd();
                }
            }

            return plaintext;
        }

        private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] initializationVector)
        {
            byte[] encrypted;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = initializationVector;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }

            return encrypted;
        }
    }
}
