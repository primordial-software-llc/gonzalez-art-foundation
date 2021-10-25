using Newtonsoft.Json;

namespace ArtApi.Model
{
    public class ClassificationLabel
    {
        [JsonProperty("confidence")]
        public float Confidence { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("parentName")]
        public string ParentName { get; set; }
    }
}
