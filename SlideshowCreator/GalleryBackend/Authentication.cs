using System;
using System.Security.Cryptography;
using System.Text;

namespace GalleryBackend
{
    public class Authentication
    {
        public static readonly string MASTER_USER_ID = "47dfa78b-9c28-41a5-9048-1df383e4c48a";

        private GalleryUserAccess GalleryUserAccess { get; }

        public Authentication(GalleryUserAccess galleryUserAccess)
        {
            GalleryUserAccess = galleryUserAccess;
        }

        public bool IsTokenValid(string token, string masterUserHash)
        {
            token = token ?? string.Empty;
            var authoritativeToken = GetToken(masterUserHash);
            return token.Equals(authoritativeToken);
        }
        
        public string GetToken(string usernamePasswordHash)
        {
            var user = GalleryUserAccess.GetUserAndUpdateSaltIfNecessary(usernamePasswordHash);
            return Hash(usernamePasswordHash + ":" + Hash(user?.TokenSaltDate + ":" + user?.TokenSalt));
        }

        public static string GetSalt()
        {
            byte[] randomBytes = new byte[32];
            using (var cryptoSecureRandomNums = new RNGCryptoServiceProvider())
            {
                cryptoSecureRandomNums.GetBytes(randomBytes, 0, randomBytes.Length);
            }
            return Convert.ToBase64String(randomBytes);
        }
        
        public static string Hash(string data)
        {
            SHA256Managed crypt = new SHA256Managed();
            byte[] hash = crypt.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }
}
