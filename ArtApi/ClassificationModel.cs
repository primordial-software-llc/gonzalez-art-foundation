﻿using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using Newtonsoft.Json;

namespace ArtApi
{
    public class ClassificationModel : IModel
    {
        public const string ARTIST_NAME_INDEX = "artistNameIndex";
        public const string NAME = "name";
        public const string SOURCE = "source";
        public const string ID = "pageId";

        [JsonProperty(SOURCE)]
        public string Source { get; set; }

        /// <summary>
        /// "source" is the same url to the site's home page.
        /// "sourceLink" is the url for the site's specific page for the work of art.
        /// Whereas the source is only an identifier of the site.
        /// </summary>
        [JsonProperty("sourceLink")]
        public string SourceLink { get; set; }

        [JsonProperty(ID)]
        public string PageId { get; set; }

        /// <summary>
        /// The artist is stripped of diacritics e.g. Jean-Leon Gerome
        /// </summary>
        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty(NAME)]
        public string Name { get; set; }

        /// <summary>
        /// The original artist has diacritics e.g. Jean-Léon Gérôme
        /// </summary>
        [JsonProperty("originalArtist")]
        public string OriginalArtist { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("s3Path")]
        public string S3Path { get; set; }

        public Dictionary<string, AttributeValue> GetKey()
        {
            return new Dictionary<string, AttributeValue>
            {
                {"source", new AttributeValue {S = Source}},
                {"pageId", new AttributeValue {N = PageId}}
            };
        }

        public string GetTable()
        {
            return "gonzalez-art-foundation-image-classification";
        }

    }
}
