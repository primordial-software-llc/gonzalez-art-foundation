using System.Collections.Generic;
using GalleryBackend.Model;
using Newtonsoft.Json;

namespace MVC5App
{
    public class ClassificationModelJson : ClassificationModel
    {
        [JsonProperty("labels")]
        public List<string> Labels { get; set; }
    }
}