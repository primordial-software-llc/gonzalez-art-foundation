using Newtonsoft.Json;

namespace IndexBackend.Sources.Rijksmuseum.Model
{
    public class Tile
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
