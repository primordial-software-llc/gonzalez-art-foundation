using Newtonsoft.Json;

namespace GalleryBackend.Model
{
    public class RequestIPAddress
    {
        [JsonProperty("ip")]
        public string IP { get; set; }

        [JsonProperty("originalVisitorIPAddress")]
        public string OriginalVisitorIPAddress { get; set; }
    }
}
