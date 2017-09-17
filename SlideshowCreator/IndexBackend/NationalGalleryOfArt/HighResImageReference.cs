using Newtonsoft.Json;

namespace IndexBackend.NationalGalleryOfArt
{
    public class HighResImageReference
    {
        [JsonProperty("mainForm")]
        public HighResImageReferenceMainForm MainForm  { get; set; }

        [JsonProperty("assets")]
        public HighResImageReferenceAssets Assets { get; set; }
    }
}
