using Newtonsoft.Json;

namespace GalleryBackend.Model
{
    public class QueueCrawlerModel
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
