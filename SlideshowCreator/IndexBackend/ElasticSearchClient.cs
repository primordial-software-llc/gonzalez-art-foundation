using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using IndexBackend.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IndexBackend
{
    public class ElasticSearchClient
    {
        protected HttpClient Client { get; }
        protected string Endpoint { get; }
        protected string ApiKey { get; }

        public ElasticSearchClient(HttpClient client, string endpoint, string apiKey)
        {
            Client = client;
            Endpoint = endpoint;
            ApiKey = apiKey;
        }

        public async Task<string> DeleteFromElasticSearch(ClassificationModel classification)
        {
            var path = "/classification/_doc/" + HttpUtility.UrlEncode($"{classification.Source}:{classification.PageId}");
            return await SendToElasticSearch(HttpMethod.Delete, path, null);
        }

        public async Task<string> SendToElasticSearch(ClassificationModel classification)
        {
            var json = JObject.Parse(JsonConvert.SerializeObject(classification, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            var path = "/classification/_doc/" + HttpUtility.UrlEncode($"{classification.Source}:{classification.PageId}");
            return await SendToElasticSearch(HttpMethod.Post, path, json);
        }

        public async Task<string> SendToElasticSearch(HttpMethod method, string path, JObject json)
        {
            var request = new HttpRequestMessage(method, new Uri($"{Endpoint}{path}"));
            request.Headers.Add("Authorization", "ApiKey " + ApiKey);
            if (json != null)
            {
                request.Content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            }
            var response = await Client.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(responseText);
            }
            response.EnsureSuccessStatusCode();
            return responseText;
        }
    }
}
