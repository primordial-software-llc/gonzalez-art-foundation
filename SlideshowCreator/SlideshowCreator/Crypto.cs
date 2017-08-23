using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace SlideshowCreator
{
    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/system.security.cryptography.aes(v=vs.110).aspx
    /// </summary>
    /// <remarks>
    /// AESCrypt wants root access to my machine. Denied. I'll take my chances with implementation weaknesses.
    /// https://www.aescrypt.com/download/
    /// </remarks>
    class Crypto
    {
        private readonly PrivateConfig privateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");

        /// <summary>
        /// We encrypt, that which we can suffer the loss of.
        /// </summary>
        //[TestCase("pick-your-poison")]
        public void Decrypt_GitHub_Recover_Codes(string password)
        {
            var recoveryCodes = File.ReadAllText(privateConfig.GithubRecoveryCodesFilePath);
            var encrypted = DecryptStringFromBytes_Aes(recoveryCodes, password, privateConfig.GithubRecoveryCodesFilePath);
            File.WriteAllText(privateConfig.GithubRecoveryCodesFilePath, encrypted);
        }

        /// <summary>
        /// CAUTION: Don't lose this password. There is no recover mechanism. Encrypt that which you can suffer the loss of.
        /// </summary>
        //[TestCase("pick-your-poison", "pick-your-poison")]
        public void Encrypt_GitHub_Recover_Codes(string passwordFirst, string passwordAgain)
        {
            var recoveryCodes = File.ReadAllText(privateConfig.GithubRecoveryCodesFilePath);
            var encrypted = Encrypt(recoveryCodes, passwordFirst, privateConfig.GithubRecoveryCodesFilePath);

            string tempEncrypted = "C:\\Users\\peon\\Desktop\\simple-copy-encrypted.txt";
            string tempDecrypted = "C:\\Users\\peon\\Desktop\\simple-copy-decrypted.txt";

            File.WriteAllText(tempEncrypted, encrypted);
            File.WriteAllText(tempDecrypted, DecryptStringFromBytes_Aes(encrypted, passwordAgain, privateConfig.GithubRecoveryCodesFilePath));

            Assert.AreEqual(File.ReadAllText("C:\\Users\\peon\\Desktop\\simple-copy-decrypted.txt"), recoveryCodes);

            File.Delete(tempDecrypted);
            File.Delete(tempEncrypted);
            File.WriteAllText(privateConfig.GithubRecoveryCodesFilePath, encrypted);
        }

        [TestCase("I just want to chew gum.", "this-is-a-secret", "another-secret-container.txt", "1Ye4sukV/bi/fTnybrQNWofrVsLPg4CxWjP+nJWGMH8=")]
        [TestCase("I just want to chew gum.", "this-is-a-secret", "unique-to-be-made-secret-container.txt", "LHcViZCsD1Mhb8K+RYLcZmVbKmgYbfPQmAkeKvfM0RE=")]
        public void Encrypt_Github_2FA_Access_Codes(string text, string keyText, string textFileName, string expectedCipher)
        {
            var encrypted = Encrypt(text, keyText, textFileName);
            Assert.AreEqual(expectedCipher, encrypted);

            var decrypted = DecryptStringFromBytes_Aes(encrypted, keyText, textFileName);

            Assert.AreEqual(text, decrypted);
        }

        public string Encrypt(string data, string keyText, string initializationVectorText)
        {
            SHA256Managed crypt = new SHA256Managed();
            byte[] key = crypt.ComputeHash(Encoding.UTF8.GetBytes(keyText));

            byte[] ivFull = crypt.ComputeHash(Encoding.UTF8.GetBytes(initializationVectorText));
            byte[] iv = new byte[16];
            Array.Copy(ivFull, iv, iv.Length);
            
            byte[] encrypted = EncryptStringToBytes_Aes(data, key, iv);
            return Convert.ToBase64String(encrypted);
        }

        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] initializationVector)
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

        static string DecryptStringFromBytes_Aes(string encrypted, string keyText, string initializationVectorText)
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

    }
}
