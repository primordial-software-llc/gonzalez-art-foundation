using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using GalleryBackend.NormalDistributionRandom;

namespace GalleryBackend
{
    public class Authentication
    {
        private string dateOfRandomness;
        private string randomness;

        private string GetUtcCalendarDay => DateTime.UtcNow.ToString("yyyy-MM-dd");
        private string RandomnessText => dateOfRandomness + ":" + randomness;

        private string IdentityHash { get; }

        public Authentication(string identityHash)
        {
            IdentityHash = identityHash;
        }

        public static string GetIdentityHash(string username, string password)
        {
            return Hash($"{username}:{password}");
        }

        public bool IsTokenValid(string token)
        {
            var authoritativeToken = GetToken(IdentityHash);
            return token.Equals(authoritativeToken);
        }

        public string GetToken(string identityHash)
        {
            TryTickRandomness();
            string saltedAuthenticationHashText = identityHash + ":" + Hash(RandomnessText);
            string saltedIdentityHash = Hash(saltedAuthenticationHashText);
            return saltedIdentityHash;
        }

        private static string Hash(string data)
        {
            SHA256Managed crypt = new SHA256Managed();
            byte[] hash = crypt.ComputeHash(Encoding.UTF8.GetBytes(data));
            string randomnessHashText = Convert.ToBase64String(hash);
            return randomnessHashText;
        }

        private void TryTickRandomness()
        {
            if (!(dateOfRandomness ?? string.Empty).Equals(GetUtcCalendarDay))
            {
                dateOfRandomness = GetUtcCalendarDay;
                randomness = new UniformRandomGenerator().NextDouble().ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
