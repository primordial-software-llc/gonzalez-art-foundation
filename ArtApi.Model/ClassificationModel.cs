using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;

namespace ArtApi.Model
{
    public class ClassificationModel : IModel
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        /// <summary>
        /// "source" is the same url to the site's home page.
        /// "sourceLink" is the url for the site's specific page for the work of art.
        /// Whereas the source is only an identifier of the site.
        /// </summary>
        [JsonProperty("sourceLink")]
        public string SourceLink { get; set; }
        
        [JsonProperty("pageId")]
        public string PageId { get; set; }

        /// <summary>
        /// The artist is stripped of diacritics e.g. Jean-Leon Gerome
        /// </summary>
        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The original artist has diacritics e.g. Jean-Léon Gérôme
        /// </summary>
        [JsonProperty(ArtistModel.ORIGINAL_ARTIST)]
        public string OriginalArtist { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("s3Bucket")]
        public string S3Bucket { get; set; }

        [JsonProperty("s3Path")]
        public string S3Path { get; set; }

        [JsonProperty("s3ThumbnailPath")]
        public string S3ThumbnailPath { get; set; }

        [JsonProperty("height")]
        public int? Height { get; set; }

        [JsonProperty("width")]
        public int? Width { get; set; }

        [JsonProperty("orientation")]
        public string Orientation { get; set; }

        [JsonProperty("price")]
        public decimal? Price { get; set; }

        [JsonProperty("priceCurrency")]
        public string PriceCurrency { get; set; }

        [JsonProperty("moderationLabels")]
        public List<ClassificationLabel> ModerationLabels { get; set; }

        [JsonProperty("nudity")]
        public bool? Nudity { get; set; }

        [JsonProperty("@timestamp")]
        public string TimeStamp { get; set; }

        public Dictionary<string, AttributeValue> GetKey()
        {
            return new Dictionary<string, AttributeValue>
            {
                {"source", new AttributeValue {S = Source}},
                {"pageId", new AttributeValue {S = PageId}}
            };
        }

        public string GetTable()
        {
            return Constants.IMAGES_TABLE;
        }

    }
}
