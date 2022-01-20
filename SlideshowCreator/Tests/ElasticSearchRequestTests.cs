using System;
using ArtApi.Model;
using NUnit.Framework;

namespace SlideshowCreator.Tests
{
    class ElasticSearchRequestTests
    {
        [Test]
        public void TestSearchJson()
        {
            var json = ElasticSearchRequest.GetSearchRequestBody(true, "source", "searchText", 100, 999);
            Console.WriteLine(json);
            var expected = @"{
  ""query"": {
    ""bool"": {
      ""must"": {
        ""multi_match"": {
          ""query"": ""searchText"",
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
                ""source.keyword"": ""source""
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
    }
}
