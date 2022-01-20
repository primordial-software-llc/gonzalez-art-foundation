using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ArtApi.Model
{
    public class ElasticSearchRequest
    {
        public static JObject GetSearchRequestBody(
            bool hideNudity,
            string source,
            string searchText,
            int maxResults,
            int searchFrom)
        {
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
                var searchQueryText = $@"{{
                    ""multi_match"": {{
                      ""query"": ""{searchText}"",
                      ""type"": ""best_fields"",
                      ""fields"": [
                        ""artist^2"",
                        ""name"",
                        ""date""
                      ]
                    }}
                }}";
                boolSearch.Add("must", JObject.Parse(searchQueryText));
            }
            boolSearch.Add("filter", JObject.Parse(filter));
            var query = new JObject { { "bool", boolSearch } };
            var request = new JObject
            {
                { "query", query },
                { "size", maxResults },
                { "from", searchFrom }
            };
            return request;
        }
    }
}
