using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;

namespace ArtApi.Routes.Unauthenticated
{
    public class GetSearchLikeArtist : IRoute
    {
        public string HttpMethod => "GET";
        public string Path => "/unauthenticated/search-like-artist";

        public void Run(APIGatewayProxyRequest request, APIGatewayProxyResponse response)
        {
            var artist = request.QueryStringParameters["artist"];
            var source = request.QueryStringParameters["source"];
            var maxResults = request.QueryStringParameters.ContainsKey("maxResults")
                ? int.Parse(request.QueryStringParameters["maxResults"])
                : 0;
            var client = new DatabaseClient<ClassificationModel>(new AmazonDynamoDBClient());
            artist = artist.ToLower();
            var queryRequest = new QueryRequest(new ClassificationModel().GetTable())
            {
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":artist", new AttributeValue {S = artist}},
                    {":source", new AttributeValue {S = source}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#source", "source" }
                },
                KeyConditionExpression = "#source = :source",
                FilterExpression = "contains(artist, :artist)"
            };
            var items = client.QueryAll(queryRequest, maxResults);
            response.Body = JsonConvert.SerializeObject(items);
        }

    }
}
