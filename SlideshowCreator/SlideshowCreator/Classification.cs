
using Newtonsoft.Json;

namespace SlideshowCreator
{
    class Classification
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("artist")]
        public string Artist { get; set; }
        [JsonProperty("date")]
        public string Date { get; set; }
        [JsonProperty("imageId")]
        public int ImageId { get; set; }
    }
}
