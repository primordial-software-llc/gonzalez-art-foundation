
using Newtonsoft.Json;

namespace GalleryBackend
{
    public class AuthenticationTokenModel
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("expirationDate")]
        public string ExpirationDate { get; set; }
    }
}