using Newtonsoft.Json;

namespace IndexBackend.NationalGalleryOfArt
{
    public class HighResImageReferenceMainForm
    {    
        [JsonProperty("project_title")]
        public string ProjectTitle => "Personal Digital Gallery";

        [JsonProperty("usage")]
        public string Usage => 5.ToString();
    }
}
