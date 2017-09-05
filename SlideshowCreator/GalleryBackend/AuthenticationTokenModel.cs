
using Newtonsoft.Json;

namespace GalleryBackend
{
    public class AuthenticationTokenModel
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("validDuring")]
        public string ValidDuring { get; set; }
    }
}