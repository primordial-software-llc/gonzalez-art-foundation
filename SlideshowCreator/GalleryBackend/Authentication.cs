using System;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon;
using Amazon.Runtime;
using Amazon.S3.Model;
using Newtonsoft.Json.Linq;

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
            JObject log = new JObject();

            string newSaltDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            if (!(salteDate ?? string.Empty).Equals(newSaltDate))
            {
                log.Add("saltDateChanged", "salt date was " + salteDate + " salt date is now " + newSaltDate);
                salteDate = newSaltDate;
                using (RNGCryptoServiceProvider cryptoSecureRandomNums = new RNGCryptoServiceProvider())
                {
                    byte[] randomBytes = new byte[32];
                    cryptoSecureRandomNums.GetBytes(randomBytes, 0, randomBytes.Length);
                    string newSalt = Convert.ToBase64String(randomBytes);
                    log.Add("saltChanged", "salt was " + base64RandomBytes + " salt is now " + newSalt);
                    base64RandomBytes = newSalt;
                }
            }
            else
            {
                log.Add("saltNotChanged", true);
            }

            var s3 = new AmazonS3Client(
                new InstanceProfileAWSCredentials(),
                RegionEndpoint.USEast1);
            s3.PutObject(new PutObjectRequest
            {
                BucketName = "tgonzalez-quick-logging",
                Key = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ"),
                ContentBody = log.ToString()
            });

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
