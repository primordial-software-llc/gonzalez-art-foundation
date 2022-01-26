using System;
using System.Net.Http;
using System.Text;
using ArtApi.Model;
using IndexBackend;
using IndexBackend.Sources.Rijksmuseum;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SlideshowCreator.Tests
{
    class ElasticSearchRequestTests
    {
        [Test]
        public void SearchAfterTest()
        {
            var elasticClient = new ElasticSearchClient(
                new HttpClient(),
                Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION"),
                Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN"));

            var result = elasticClient.SendToElasticSearch(
                HttpMethod.Get,
                "/classification/_search",
                ElasticSearchRequest.GetSearchRequestBody(
                    true,
                    "",
                    "Sir Lawrence Alma-Tadema",
                    100,
                    null)
                ).Result;

            var resultAfter = elasticClient.SendToElasticSearch(
                HttpMethod.Get,
                "/classification/_search",
                ElasticSearchRequest.GetSearchRequestBody(
                    true,
                    "",
                    "Sir Lawrence Alma-Tadema",
                    100,
                    JObject.Parse(result)["hits"]["hits"].Last["sort"]
                )
            ).Result;
        }

        /*
        [Test]
        public void TestSearchJson()
        {
            var json = ElasticSearchRequest.GetSearchRequestBody(true, RijksmuseumIndexer.Source, "lawrence", 100, 999);
            Console.WriteLine(json);
            var expected = @"{
  ""query"": {
    ""bool"": {
      ""must"": {
        ""multi_match"": {
          ""query"": ""lawrence"",
          ""type"": ""best_fields"",
          ""fields"": [
            ""artist^2"",
            ""name"",
            ""date""
          ]
        }
      },
      ""filter"": {
        ""bool"": {
          ""must"": [
            {
              ""bool"": {
                ""should"": [
                  {
                    ""term"": {
                      ""nudity"": ""false""
                    }
                  },
                  {
                    ""bool"": {
                      ""must_not"": {
                        ""exists"": {
                          ""field"": ""nudity""
                        }
                      }
                    }
                  }
                ]
              }
            },
            {
              ""term"": {
                ""source.keyword"": ""https://www.rijksmuseum.nl""
              }
            }
          ]
        }
      }
    }
  },
  ""size"": 100,
  ""from"": 999
}";
            Assert.AreEqual(expected, json.ToString());
        }
        */
    }
}
