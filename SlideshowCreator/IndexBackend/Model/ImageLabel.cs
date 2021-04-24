using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;

namespace IndexBackend.Model
{
    public class ImageLabel : IModel
    {
        [JsonProperty("s3Path")]
        public string S3Path { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("pageId")]
        public int PageId { get; set; }

        [JsonProperty("labels")]
        public List<string> LabelsAndConfidence { get; set; }

        [JsonProperty("normalizedLabels")]
        public List<string> NormalizedLabels { get; set; }

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
