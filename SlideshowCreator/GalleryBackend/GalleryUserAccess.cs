using Amazon.DynamoDBv2;
using AwsTools;
using GalleryBackend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;

namespace GalleryBackend
{
    public class GalleryUserAccess
    {
        private IAmazonDynamoDB Client { get; }
        private IDynamoDbClient<GalleryUser> AwsToolsClient { get; }
        private ILogging Logging { get; }
        private ILogging AccessLogging { get; }

        public GalleryUserAccess(IAmazonDynamoDB client, ILogging logging, IDynamoDbClient<GalleryUser> awsToolsClient, ILogging accessLogging)
        {
            Client = client;
            Logging = logging;
            AwsToolsClient = awsToolsClient;
            AccessLogging = accessLogging;
        }

        public GalleryUser GetUserAndUpdateSaltIfNecessary(string usernamePasswordHash)
        {
            Dictionary<string, AttributeValue> userHashIndexKey = Conversion<GalleryUser>.ConvertToDynamoDb(new GalleryUser {Hash = usernamePasswordHash});
            var user = AwsToolsClient.Get(GalleryUser.USER_HASH_INDEX, userHashIndexKey).Result;

            if (user == null)
            {
                return null;
            }
            
            string newSaltDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
            if (!(user.TokenSaltDate ?? string.Empty).Equals(newSaltDate))
            {
                JObject log = new JObject
                {
                    {"saltDateChanged", "salt date was " + user.TokenSaltDate + " salt date is now " + newSaltDate}
                };
                
                var newSalt = Authentication.GetSalt();
                log.Add("saltChanged", "salt was " + user.TokenSalt + " salt is now " + newSalt);
                user.TokenSalt = newSalt;
                user.TokenSaltDate = newSaltDate;

                Task.WaitAll(AwsToolsClient.Insert(user));
                log.Add("updatedGalleryUser", JsonConvert.SerializeObject(user));

                AccessLogging.Log(log.ToString());
            }

            return user;
        }

    }
}
