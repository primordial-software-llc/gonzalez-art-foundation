using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using GalleryBackend.Model;
using Newtonsoft.Json;

namespace GalleryBackend
{
    public class GalleryClient
    {
        public string Token { get; }
        public HttpClient Client { get; }

        public GalleryClient(string username, string password)
        {
            Client = new HttpClient();
            
            var url = $"https://tgonzalez.net/api/Gallery/token?username={username}&password={password}";

            var response = Client.GetStringAsync(url).Result;
            var parsedResponse = JsonConvert.DeserializeObject<AuthenticationTokenModel>(response);
            Token = parsedResponse.Token;
        }

        public RequestIPAddress GetIPAddress()
        {
            var url = $"https://tgonzalez.net/api/Gallery/ip?token={HttpUtility.UrlEncode(Token)}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<RequestIPAddress>(response);
        }

        public WaitTime GetWaitTime(int waitTimeInMilliseconds)
        {
            var url = "https://tgonzalez.net/api/Gallery/wait" +
                $"?token={HttpUtility.UrlEncode(Token)}" +
                $"&waitInMilliseconds={waitTimeInMilliseconds}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<WaitTime>(response);
        }

        public List<ClassificationModel> SearchExactArtist(string artist)
        {
            var url = $"https://tgonzalez.net/api/Gallery/searchExactArtist?token={HttpUtility.UrlEncode(Token)}&artist={artist}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
        }

        public List<ClassificationModel> SearchLikeArtist(string artist)
        {
            var url = $"https://tgonzalez.net/api/Gallery/searchLikeArtist?token={HttpUtility.UrlEncode(Token)}&artist={artist}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
        }

        public List<ClassificationModel> Scan(int lastPageId = 0)
        {
            var url = $"https://tgonzalez.net/api/Gallery/scan?token={HttpUtility.UrlEncode(Token)}&lastPageId={lastPageId}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
        }

    }
}
