using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using Newtonsoft.Json;

namespace GalleryBackend.Model
{
    public class ImageLabel : IModel
    {
        [JsonProperty("s3Path")]
        public string S3Path { get; set; }

        [JsonProperty(ClassificationModel.SOURCE)]
        public string Source { get; set; }

        [JsonProperty(ClassificationModel.ID)]
        public int PageId { get; set; }

        [JsonProperty("Labels")]
        public List<string> LabelsAndConfidence { get; set; }

        [JsonProperty("normalizedLabels")]
        public List<string> NormalizedLabels { get; set; }

        // Data needs to be cleaned up.
        // [JsonProperty("originalLabels")]
        // public List<string> OriginalLabels { get; set; }

        public Dictionary<string, AttributeValue> GetKey()
        {
            return new Dictionary<string, AttributeValue>
            {
                {"source", new AttributeValue {S = Source}},
                {"pageId", new AttributeValue {N = PageId.ToString()}}
            };
        }

        public string GetTable()
        {
            return "ImageLabel";
        }
    }
}
