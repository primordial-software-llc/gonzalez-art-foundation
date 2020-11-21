using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web;
using GalleryBackend.Model;
using Newtonsoft.Json;

namespace GalleryBackend
{
    public class GalleryClient
    {
        public HttpClient Client { get; }
        public string Domain { get; }

        public GalleryClient(string domain, string username, string password)
        {
            Domain = domain;
            var url = $"https://{Domain}/api/Gallery/token" +
                      $"?username={HttpUtility.UrlEncode(username)}" +
                      $"&password={HttpUtility.UrlEncode(password)}";

            CookieContainer cookies = new CookieContainer();
            HttpClientHandler cookieHandler = new HttpClientHandler { CookieContainer = cookies };
            Client = new HttpClient(cookieHandler);
            
            var response = Client.GetStringAsync(url).Result;
            var parsedResponse = JsonConvert.DeserializeObject<AuthenticationTokenModel>(response);
            cookies.Add(new Cookie("token", HttpUtility.UrlEncode(parsedResponse.Token)) {Domain = domain});
        }

        public RequestIPAddress GetIPAddress()
        {
            var url = $"https://{Domain}/api/Gallery/ip";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<RequestIPAddress>(response);
        }

        public List<ClassificationModelNew> SearchExactArtist(string artist, string source)
        {
            var url = $"https://{Domain}/api/Gallery/searchExactArtist" +
                      $"?artist={HttpUtility.UrlEncode(artist)}" +
                      $"&source={HttpUtility.UrlEncode(source)}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ClassificationModelNew>>(response);
        }

        public List<ClassificationModelNew> SearchLikeArtist(string artist, string source)
        {
            var url = $"https://{Domain}/api/Gallery/searchLikeArtist" +
                      $"?artist={HttpUtility.UrlEncode(artist)}"+
                      $"&source={HttpUtility.UrlEncode(source)}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ClassificationModelNew>>(response);
        }

        public List<ClassificationModelNew> Scan(int? lastPageId, string source)
        {
            var url = $"https://{Domain}/api/Gallery/scan" +
                      $"?lastPageId={lastPageId.GetValueOrDefault()}" +
                      $"&source={HttpUtility.UrlEncode(source)}";

            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ClassificationModelNew>>(response);
        }

        public List<ImageLabel> SearchLabel(string label, string source)
        {
            var url = $"https://{Domain}/api/Gallery/searchLabel" +
                      $"?label={HttpUtility.UrlEncode(label)}" +
                      $"&source={HttpUtility.UrlEncode(source)}";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ImageLabel>>(response);
        }

        public List<ImageLabel> GetImageLabels(int pageId)
        {
            var url = $"https://{Domain}/api/Gallery/{pageId}/labels";
            var response = Client.GetStringAsync(url).Result;
            return JsonConvert.DeserializeObject<List<ImageLabel>>(response);
        }

        public HttpContent GetImage(string s3Path)
        {
            var url = $"https://{Domain}/api/Gallery/image/{s3Path}";
            var response = Client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            return response.Content;
        }

    }
}
