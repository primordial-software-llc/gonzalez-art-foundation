
using Newtonsoft.Json;

namespace GalleryBackend.Model
{
    public class WaitTime
    {
        [JsonProperty("waitInMilliseconds")]
        public int WaitInMilliseconds { get; set; }
    }
}
