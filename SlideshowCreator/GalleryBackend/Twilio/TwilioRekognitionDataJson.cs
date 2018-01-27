using Newtonsoft.Json;

namespace GalleryBackend.Twilio
{
    public class TwilioRekognitionDataJson
    {
        [JsonProperty("messageCount")]
        public int MessageCount { get; set; }
    }
}
