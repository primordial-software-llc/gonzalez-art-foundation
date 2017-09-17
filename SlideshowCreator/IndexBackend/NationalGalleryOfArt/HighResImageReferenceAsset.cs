using Newtonsoft.Json;

namespace IndexBackend.NationalGalleryOfArt
{
    public class HighResImageReferenceAsset
    {
        [JsonProperty("assetId")]
        public string AssetId { get; set; }

        [JsonProperty("sizeId")]
        public string SizeId => 3.ToString();
    }
}
