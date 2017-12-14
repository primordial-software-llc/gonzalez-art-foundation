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
        public string Domain { get; }

        public GalleryClient(string domain, string username, string password)
        {
            Domain = domain;
            var url = $"https://{Domain}/api/Gallery/token" +
                      $"?username={HttpUtility.UrlEncode(username)}" +
                      $"&password={HttpUtility.UrlEncode(password)}";
            Client = new HttpClient();
            var response = Client.GetStringAsync(url).Result;
            var parsedResponse = JsonConvert.DeserializeObject<AuthenticationTokenModel>(response);
            Token = parsedResponse.Token;
        }

        public RequestIPAddress GetIPAddress()
        {
            var url = $"https://{Domain}/api/Gallery/ip" +
                      $"?token={HttpUtility.UrlEncode(Token)}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<RequestIPAddress>(response);
        }

        public List<ClassificationModel> SearchExactArtist(string artist, string source)
        {
            var url = $"https://{Domain}/api/Gallery/searchExactArtist" +
                      $"?token={HttpUtility.UrlEncode(Token)}" +
                      $"&artist={HttpUtility.UrlEncode(artist)}" +
                      $"&source={HttpUtility.UrlEncode(source)}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
        }

        public List<ClassificationModel> SearchLikeArtist(string artist, string source)
        {
            var url = $"https://{Domain}/api/Gallery/searchLikeArtist" +
                      $"?token={HttpUtility.UrlEncode(Token)}" +
                      $"&artist={HttpUtility.UrlEncode(artist)}"+
                      $"&source={HttpUtility.UrlEncode(source)}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
        }

        public List<ClassificationModel> Scan(int? lastPageId, string source)
        {
            var url = $"https://{Domain}/api/Gallery/scan" +
                      $"?token={HttpUtility.UrlEncode(Token)}" +
                      $"&lastPageId={lastPageId.GetValueOrDefault()}" +
                      $"&source={HttpUtility.UrlEncode(source)}";

            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
        }

        public List<ImageLabel> SearchLabel(string label, string source)
        {
            var url = $"https://{Domain}/api/Gallery/searchLabel" +
                      $"?token={HttpUtility.UrlEncode(Token)}" +
                      $"&label={HttpUtility.UrlEncode(label)}" +
                      $"&source={HttpUtility.UrlEncode(source)}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ImageLabel>>(response);
        }

        public List<ImageLabel> GetImageLabels(int pageId)
        {
            var url = $"https://{Domain}/api/Gallery/{pageId}/labels" +
                      $"?token={HttpUtility.UrlEncode(Token)}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ImageLabel>>(response);
        }

    }
}
