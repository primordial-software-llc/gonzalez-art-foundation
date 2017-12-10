
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using Newtonsoft.Json;

namespace GalleryBackend.Model
{
    public class ClassificationModel : IModel
    {
        public const string SOURCE = "source";
        public const string ID = "pageId"; // Should just be "id", because NGA uses the term "assetId" and loads pages based on asset and size. Whereas the-athenaeum uses a pageId then an imageId for the "asset" on the page.
        public const string ORIGINAL_ARTIST = "originalArtist";

        [JsonProperty(SOURCE)]
        public string Source { get; set; }
        
        [JsonProperty(ID)]
        public int PageId { get; set; }

        /// <summary>
        /// The artist is stripped of diacritics e.g. Jean-Leon Gerome
        /// </summary>
        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("imageId")]
        public int ImageId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The original artist has diacritics e.g. Jean-Léon Gérôme
        /// </summary>
        [JsonProperty(ORIGINAL_ARTIST)]
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
                {"pageId", new AttributeValue {N = PageId.ToString()}}
            };
        }

        string IModel.GetTable()
        {
            return ImageClassification.TABLE_IMAGE_CLASSIFICATION;
        }

    }
}
