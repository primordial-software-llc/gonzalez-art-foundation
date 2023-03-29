using System;
using ArtApi.Model;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SlideshowCreator.Tests
{
    class ElasticSearchRequestTests
    {

        [Test]
        public void TestSearchJson()
        {
            var json = ElasticSearchRequest.GetSearchRequestBody(Constants.SOURCE_RIJKSMUSEUM, "lawrence", 100, null, false);
            Console.WriteLine(json);
            var expected = @"{
  ""track_total_hits"": true,
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
  ""sort"": [
    {
      ""_score"": {
        ""order"": ""desc""
      }
    },
    {
      ""source.keyword"": {
            ""order"": ""asc""
        }
    },
    {
      ""pageId.keyword"": {
            ""order"": ""asc""
        }
    }
  ]
}";
            Assert.AreEqual(JObject.Parse(expected).ToString(), json.ToString());
        }

        [Test]
        public void TestExactArtistMatch()
        {
            var json = ElasticSearchRequest.GetSearchRequestBody(
                string.Empty,
                "sir lawrence alma-tadema",
                100,
                null,
                true);
            Console.WriteLine(json);
            var expected = @"{
  ""track_total_hits"": true,
  ""query"": {
    ""bool"": {
      ""must"": {
        ""multi_match"": {
          ""query"": ""sir lawrence alma-tadema"",
          ""type"": ""best_fields"",
          ""fields"": [
            ""artist.keyword""
          ]
        }
      },
      ""filter"": {
        ""bool"": {
          ""must"": []
        }
      }
    }
  },
  ""size"": 100,
  ""sort"": [
    {
      ""_score"": {
        ""order"": ""desc""
      }
    },
    {
      ""source.keyword"": {
            ""order"": ""asc""
        }
    },
    {
      ""pageId.keyword"": {
            ""order"": ""asc""
        }
    }
  ]
}";
            Assert.AreEqual(JObject.Parse(expected).ToString(), json.ToString());
        }
        
    }
}
