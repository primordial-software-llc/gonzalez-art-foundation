using Newtonsoft.Json;

namespace SlideshowCreator
{
    class MonaLisaDatabaseFieldsModel
    {
        [JsonProperty("museo")]
        public string Museo { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("tech")]
        public string Tech { get; set; }

        [JsonProperty("inv")]
        public string Inv { get; set; }

        [JsonProperty("domn")]
        public string Domn { get; set; }
    }
}
