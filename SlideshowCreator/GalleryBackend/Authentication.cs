﻿using System;
using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GalleryBackend
{
    public class Authentication
    {
        private IAmazonS3 S3Client { get; }
        private IDynamoDbClient<GalleryUser> UserClient { get; }

        public Authentication(IAmazonS3 s3Client, IDynamoDbClient<GalleryUser> userClient)
        {
            S3Client = s3Client;
            UserClient = userClient;
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
            var user = UserClient.Get(new GalleryUser { Id = "47dfa78b-9c28-41a5-9048-1df383e4c48a" }).Result;

            string newSaltDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            if (!(user.TokenSaltDate ?? string.Empty).Equals(newSaltDate))
            {
                log.Add("saltDateChanged", "salt date was " + user.TokenSaltDate + " salt date is now " + newSaltDate);
                user.TokenSaltDate = newSaltDate;

                using (RNGCryptoServiceProvider cryptoSecureRandomNums = new RNGCryptoServiceProvider())
                {
                    byte[] randomBytes = new byte[32];
                    cryptoSecureRandomNums.GetBytes(randomBytes, 0, randomBytes.Length);
                    log.Add("saltChanged", "salt was " + user.TokenSalt + " salt is now " + user.TokenSalt);
                    user.TokenSalt = Convert.ToBase64String(randomBytes);
                }
                
                UserClient.Insert(user).Wait();
                log.Add("Gallery user salt updated: " + JsonConvert.SerializeObject(user));
            }
            else
            {
                log.Add("saltNotChanged", true);
            }

            S3Client.PutObject(new PutObjectRequest
            {
                BucketName = "tgonzalez-quick-logging",
                Key = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ"),
                ContentBody = log.ToString()
            });

            return Hash(identityHash + ":" + Hash(user.TokenSaltDate + ":" + user.TokenSalt));
        }
        
        public static string Hash(string data)
        {
            SHA256Managed crypt = new SHA256Managed();
            byte[] hash = crypt.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }
}
