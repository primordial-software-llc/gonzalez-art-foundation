using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;

namespace ArtApi.Model
{
    public class ArtistModel : IModel
    {
        public const string ARTIST = "artist";
        public const string ORIGINAL_ARTIST = "originalArtist";
        public const string NUMBER_OF_WORKS = "numberOfWorks";

        /// <summary>
        /// The artist is stripped of diacritics and lowered for search e.g. jean-leon gerome
        /// </summary>
        [JsonProperty(ARTIST)]
        public string Artist { get; set; }

        /// <summary>
        /// The original artist has diacritics e.g. Jean-Léon Gérôme
        /// </summary>
        [JsonProperty(ORIGINAL_ARTIST)]
        public string OriginalArtist { get; set; }

        /// <summary>
        /// The original artist has diacritics e.g. Jean-Léon Gérôme
        /// </summary>
        [JsonProperty(NUMBER_OF_WORKS)]
        public int NumberOfWorks { get; set; }

        public Dictionary<string, AttributeValue> GetKey()
        {
            return new Dictionary<string, AttributeValue>
            {
                {ARTIST, new AttributeValue {S = Artist }}
            };
        }

        public string GetTable()
        {
            return "gonzalez-art-foundation-artist";
        }
    }
}
