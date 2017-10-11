using System;
using Newtonsoft.Json;

namespace GalleryBackend.Model
{
    public class BackpageAdModel
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("url")]
        public Uri Uri { get; set; }

        [JsonProperty("age")]
        public int Age { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }
    }
}
