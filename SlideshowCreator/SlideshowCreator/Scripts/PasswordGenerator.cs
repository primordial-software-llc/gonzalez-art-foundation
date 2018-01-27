using System;
using System.Security.Cryptography;
using NUnit.Framework;

namespace SlideshowCreator.Scripts
{
    class PasswordGenerator
    {
        [Test]
        public void CreatePassword()
        {
            byte[] randomBytes = new byte[32];
            using (var cryptoSecureRandomNums = new RNGCryptoServiceProvider())
            {
                cryptoSecureRandomNums.GetBytes(randomBytes, 0, randomBytes.Length);
            }
            Console.WriteLine(Convert.ToBase64String(randomBytes));
        }
    }
}
