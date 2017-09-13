
using Newtonsoft.Json;

namespace IndexBackend
{
    public class ClassificationModel
    {
        public const string ORIGINAL_ARTIST = "originalArtist";

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("pageId")]
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
    }
}
