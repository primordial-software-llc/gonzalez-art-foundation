using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ArtApi.Model
{
    public class ElasticSearchRequest
    {
        public static JObject GetSearchRequestBody(
            string source,
            string searchText,
            int maxResults,
            JToken searchAfter,
            bool artistExactMatch)
        {
            var filters = new List<string>();
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
            {{
              ""bool"": {{
                ""must"": [
                  {string.Join(",", filters)}
                ]
              }}
            }}";
            var boolSearch = new JObject();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                List<string> fields = artistExactMatch
                    ? new List<string> { "\"artist.keyword\"" }
                    : new List<string> { "\"artist^2\"", "\"name\"", "\"date\"" };
                
                var searchQueryText = $@"{{
                        ""multi_match"": {{
                          ""query"": ""{searchText}"",
                          ""type"": ""best_fields"",
                          ""fields"": [
                              {string.Join(",", fields)}
                          ]
                        }}
                    }}";
                boolSearch.Add("must", JObject.Parse(searchQueryText));
            }
            boolSearch.Add("filter", JObject.Parse(filter));
            var query = new JObject { { "bool", boolSearch } };
            var sort = JToken.Parse(@"[ 
                { ""_score"": {""order"": ""desc""}},
                { ""source.keyword"": { ""order"":""asc""} },
                { ""pageId.keyword"": { ""order"":""asc""} }
            ]");
            var request = new JObject
            {
                { "track_total_hits", true }, // Turn off to improve search performance: https://www.elastic.co/guide/en/elasticsearch/reference/current/search-your-data.html#track-total-hits
                { "query", query },
                { "size", maxResults },
                { "sort", sort }
            };
            if (searchAfter != null && searchAfter.Any())
            {
                request.Add("search_after", searchAfter);
            }
            return request;
        }
    }
}
