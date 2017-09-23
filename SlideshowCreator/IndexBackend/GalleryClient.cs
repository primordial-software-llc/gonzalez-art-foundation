using System.Net;
using System.Web;
using GalleryBackend;
using GalleryBackend.Model;
using Newtonsoft.Json;

namespace IndexBackend
{
    public class GalleryClient
    {
        public AuthenticationTokenModel Authenticate(string username, string password)
        {
            var url = $"https://tgonzalez.net/api/Gallery/token?username={username}&password={password}";
            var response = new WebClient().DownloadString(url);
            return JsonConvert.DeserializeObject<AuthenticationTokenModel>(response);
        }

        public RequestIPAddress GetIPAddress(string token)
        {
            var url = $"https://tgonzalez.net/api/Gallery/ip?token={HttpUtility.UrlEncode(token)}";
            var response = new WebClient().DownloadString(url);
            return JsonConvert.DeserializeObject<RequestIPAddress>(response);
        }
    }
}
