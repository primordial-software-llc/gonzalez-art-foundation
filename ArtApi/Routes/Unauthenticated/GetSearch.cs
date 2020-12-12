using System;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Web;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ArtApi.Routes.Unauthenticated
{
    public class GetSearch : IRoute
    {
        public string HttpMethod => "GET";
        public string Path => "/unauthenticated/search";

        public void Run(APIGatewayProxyRequest request, APIGatewayProxyResponse response)
        {
            var searchText = request.QueryStringParameters.ContainsKey("searchText")
                ? HttpUtility.JavaScriptStringEncode(request.QueryStringParameters["searchText"].Trim())
                : string.Empty;
            const int maxResultsLimit = 5000;
            var maxResults = request.QueryStringParameters.ContainsKey("maxResults")
                ? int.Parse(request.QueryStringParameters["maxResults"])
                : maxResultsLimit;
            maxResults = maxResults <= 0 ? maxResultsLimit : maxResults;

            var getRequest = @$"{{
              ""query"": {{
                ""multi_match"" : {{
                  ""query"": ""{searchText.Trim()}"", 
                  ""fields"": [
                    ""artist"",
                    ""name"",
                    ""date"",
                    ""source"",
                    ""sourceLink""
                  ]
                }}
              }},
              ""size"": {maxResults}
            }}";

            var elasticSearchResponse = SendToElasticSearch(
                new HttpClient(),
                System.Net.Http.HttpMethod.Get,
                "/classification/_search",
                JObject.Parse(getRequest));

            var responseJson = JObject.Parse(elasticSearchResponse);
            var items = responseJson["hits"]["hits"].Select(x => x["_source"]).ToList();
            response.Body = JsonConvert.SerializeObject(items);
        }

        public string SendToElasticSearch(HttpClient client, HttpMethod method, string path, JObject json)
        {
            var apiKey = Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN");

            var request = new HttpRequestMessage(
                method,
                new Uri($"{Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION")}{path}"));
            if (json != null)
            {
                request.Content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            }
            request.Headers.Add("Authorization", "ApiKey " + apiKey);

            var response = client.SendAsync(request).Result;

            if (!response.IsSuccessStatusCode)
            {
                var dataBack = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(dataBack);
            }
            response.EnsureSuccessStatusCode();
            var responseText = response.Content.ReadAsStringAsync().Result;
            return responseText;
        }
    }
}
