using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using ArtApi.Model;

namespace ArtApi.Routes.Unauthenticated.CacheEverything
{
    class GetImageClassification : IRoute
    {
        public string HttpMethod => "GET";
        public string Path => "/unauthenticated/cache-everything/image-classification";

        public void Run(APIGatewayProxyRequest request, APIGatewayProxyResponse response)
        {
            var source = request.QueryStringParameters["source"];
            var pageId = request.QueryStringParameters["pageId"];
            var queryRequest = new QueryRequest(new ClassificationModel().GetTable())
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":source", new AttributeValue {S = source}},
                    {":pageId", new AttributeValue {S = pageId}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#source", "source"},
                    {"#pageId", "pageId"}
                },
                KeyConditionExpression = "#source = :source AND #pageId = :pageId",
                Limit = 1
            };
            var client = new AmazonDynamoDBClient();
            var queryResponse = client.QueryAsync(queryRequest).Result;
            var item = queryResponse.Items.FirstOrDefault();
            response.Body = Document.FromAttributeMap(item).ToJson();
        }
    }
}
