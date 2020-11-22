using Newtonsoft.Json;

namespace SlideshowCreator
{
    class MonaLisaDatabaseFieldsModel
    {
        [JsonProperty("museo")]
        public string Museo { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }
    }
}
