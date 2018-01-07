using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using Newtonsoft.Json;

namespace GalleryBackend.Model
{
    public class GalleryUser : IModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("tokenSaltDate")]
        public string TokenSaltDate { get; set; }

        [JsonProperty("tokenSalt")]
        public string TokenSalt { get; set; }

        public const string USER_HASH_INDEX = "UserHashIndex";

        public Dictionary<string, AttributeValue> GetKey()
        {
            return new Dictionary<string, AttributeValue>
            {
                {"id", new AttributeValue {S = Id}}
            };
        }

        public string GetTable()
        {
            return "GalleryUser";
        }
    }
}
