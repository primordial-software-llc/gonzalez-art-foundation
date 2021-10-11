using System;
using System.Collections.Generic;
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
            const int maxResultsLimit = 500;
            var maxResults = request.QueryStringParameters.ContainsKey("maxResults")
                ? int.Parse(request.QueryStringParameters["maxResults"])
                : maxResultsLimit;
            maxResults = maxResults <= 0 || maxResults >= maxResultsLimit
                ? maxResultsLimit
                : maxResults;
            var searchFrom = request.QueryStringParameters.ContainsKey("searchFrom")
                ? int.Parse(request.QueryStringParameters["searchFrom"])
                : 0;
            var source = request.QueryStringParameters.ContainsKey("source")
                ? HttpUtility.JavaScriptStringEncode(request.QueryStringParameters["source"].Trim())
                : string.Empty;
            var hideNudity = request.QueryStringParameters.ContainsKey("hideNudity") && bool.Parse(request.QueryStringParameters["hideNudity"].Trim());
            Console.WriteLine("Hide nudity: " + hideNudity);
            var filters = new List<string>();
            if (hideNudity)
            {
                filters.Add($@"
                {{
                  ""bool"": {{
                    ""should"": [
                      {{
                        ""term"": {{
                          ""nudity"": ""false""
                        }}
                      }},
                      {{
                        ""bool"": {{
                          ""must_not"": {{
                            ""exists"": {{
                              ""field"": ""nudity""
                            }}
                          }}
                        }}
                      }}
                    ]
                  }}
                }}
                ");
            }
            if (!string.IsNullOrWhiteSpace(source))
            {
                filters.Add($@"
                  {{
                    ""term"": {{
                      ""source.keyword"": ""{source}""
                    }}
                  }}
                ");
            }
            var filter = $@"
            ,""filter"": {{
              ""bool"": {{
                ""must"": [
                  {string.Join(",", filters)}
                ]
              }}
            }}";
            var getRequest = $@"{{
              ""query"": {{
                ""bool"": {{
                  ""must"": {{
                    ""multi_match"": {{
                      ""query"": ""{searchText}"",
                      ""type"": ""best_fields"",
                      ""fields"": [
                        ""artist^2"",
                        ""name"",
                        ""date""
                      ]
                    }}
                  }}
                  { filter }
                }}
              }},
              ""size"": {maxResults},
              ""from"": {searchFrom}
            }}";
            var elasticSearchResponse = SendToElasticSearch(
                new HttpClient(),
                System.Net.Http.HttpMethod.Get,
                "/classification/_search",
                JObject.Parse(getRequest));
            var responseJson = JObject.Parse(elasticSearchResponse);
            var searchResult = new JObject
            {
                { "items", JToken.FromObject(responseJson["hits"]["hits"].Select(x => x["_source"]).ToList()) },
                { "total", responseJson["hits"]["total"]["value"] },
                { "maxSearchResultsHit", string.Equals(responseJson["hits"]["total"]["relation"].Value<string>(), "gte", StringComparison.OrdinalIgnoreCase) }
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
