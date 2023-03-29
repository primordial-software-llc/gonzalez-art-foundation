using System;
using System.Net.Http;
using System.Text;
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
            const int MAX_RESULTS_LIMIT = 500;
            request.QueryStringParameters.TryGetValue("maxResults", out var maxResultsText);
            int.TryParse(maxResultsText, out var maxResults);
            maxResults = maxResults <= 0 || maxResults >= MAX_RESULTS_LIMIT
                ? MAX_RESULTS_LIMIT
                : maxResults;
            JArray searchAfterParsed = null;
            request.QueryStringParameters.TryGetValue("searchAfter", out var searchAfter);
            if (!string.IsNullOrWhiteSpace(searchAfter))
            {
                searchAfterParsed = JArray.Parse(searchAfter);
            }
            var source = request.QueryStringParameters.ContainsKey("source")
                ? HttpUtility.JavaScriptStringEncode(request.QueryStringParameters["source"].Trim())
                : string.Empty;
            var artistExactMatchRaw = request.QueryStringParameters.ContainsKey("artistExactMatch")
                ? HttpUtility.JavaScriptStringEncode(request.QueryStringParameters["artistExactMatch"].Trim())
                : bool.FalseString;
            bool.TryParse(artistExactMatchRaw, out var artistExactMatch);
            var getRequest = Model.ElasticSearchRequest.GetSearchRequestBody(
                source,
                searchText,
                maxResults,
                searchAfterParsed,
                artistExactMatch);
            var elasticSearchResponse = SendToElasticSearch(
                new HttpClient(),
                System.Net.Http.HttpMethod.Get,
                "/classification/_search",
                getRequest);
            var responseJson = JObject.Parse(elasticSearchResponse);
            var searchResult = new SearchResult
            {
                Items = responseJson["hits"]["hits"],
                Total = responseJson["hits"]["total"]["value"].Value<int>(),
                Source = source,
                SearchText = searchText,
                SearchAfter = searchAfterParsed,
                MaxResults = maxResults
            };
            response.Body = JsonConvert.SerializeObject(searchResult);
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
