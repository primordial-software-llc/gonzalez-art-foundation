using System;
using System.Security.Cryptography;
using System.Text;

namespace GalleryBackend
{
    public class Authentication
    {
        public static readonly Authentication SINGLETON = new Authentication();

        protected string salteDate;
        protected string base64RandomBytes;

        protected Authentication()
        {
            // Use singleton. Extend to test. In production only one can exist.
        }

        public bool IsTokenValid(string token, string authoritativeHash)
        {
            token = token ?? string.Empty;
            var authoritativeToken = GetToken(authoritativeHash);
            return token.Equals(authoritativeToken);
        }

        public string GetToken(string identityHash)
        {
            if (!(salteDate ?? string.Empty).Equals(DateTime.UtcNow.Date.ToString("yyyy-MM-dd")))
            {
                salteDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
                using (RNGCryptoServiceProvider cryptoSecureRandomNums = new RNGCryptoServiceProvider())
                {
                    byte[] randomBytes = new byte[32];
                    cryptoSecureRandomNums.GetBytes(randomBytes, 0, randomBytes.Length);
                    base64RandomBytes = Convert.ToBase64String(randomBytes);
                }
            }
            return Hash(identityHash + ":" + Hash(salteDate + ":" + base64RandomBytes));
        }
        
        public static string Hash(string data)
        {
            SHA256Managed crypt = new SHA256Managed();
            byte[] hash = crypt.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }
}
