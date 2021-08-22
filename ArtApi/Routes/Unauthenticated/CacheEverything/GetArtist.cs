using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;

namespace ArtApi.Routes.Unauthenticated.CacheEverything
{
    class GetArtist : IRoute
    {
        public string HttpMethod => "GET";
        public string Path => "/unauthenticated/cache-everything/artist";

        public void Run(APIGatewayProxyRequest request, APIGatewayProxyResponse response)
        {
            var items = new List<ArtistModel>();
            var client = new AmazonDynamoDBClient();
            var scanRequest = new ScanRequest(new ArtistModel().GetTable());
            ScanResponse queryResponse = null;
            do
            {
                if (queryResponse != null)
                {
                    scanRequest.ExclusiveStartKey = queryResponse.LastEvaluatedKey;
                }
                queryResponse = client.ScanAsync(scanRequest).Result;
                foreach (var item in queryResponse.Items)
                {
                    var model = JsonConvert.DeserializeObject<ArtistModel>(Document.FromAttributeMap(item).ToJson());
                    items.Add(model);
                }
            } while (queryResponse.LastEvaluatedKey.Any());
            response.Body = JsonConvert.SerializeObject(items);
        }

    }
}
