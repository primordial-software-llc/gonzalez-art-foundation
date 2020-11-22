using Newtonsoft.Json;

namespace SlideshowCreator
{
    class MonaLisaDatabaseModel
    {
        [JsonProperty("fields")]
        public MonaLisaDatabaseFieldsModel Fields { get; set; }
    }
}
