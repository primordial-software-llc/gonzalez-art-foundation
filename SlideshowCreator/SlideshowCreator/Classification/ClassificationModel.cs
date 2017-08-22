
using Newtonsoft.Json;

namespace SlideshowCreator.Classification
{
    class ClassificationModel
    {
        [JsonProperty("pageId")]
        public int PageId { get; set; }

        [JsonProperty("imageId")]
        public int ImageId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }
    }
}
