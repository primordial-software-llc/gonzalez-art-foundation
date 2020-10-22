﻿using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Newtonsoft.Json;

namespace ArtApi.Routes.Unauthenticated
{
    class GetScan : IRoute
    {
        public string HttpMethod => "GET";
        public string Path => "/unauthenticated/scan";

        public void Run(APIGatewayProxyRequest request, APIGatewayProxyResponse response)
        {
            var lastPageId = request.QueryStringParameters["lastPageId"];
            var source = request.QueryStringParameters["source"];
            var queryRequest = new QueryRequest(new ClassificationModel().GetTable())
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":source", new AttributeValue {S = source}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#source", "source"}
                },
                KeyConditionExpression = "#source = :source",
                ExclusiveStartKey = new Dictionary<string, AttributeValue>
                {
                    {"source", new AttributeValue {S = source}},
                    {"pageId", new AttributeValue {N = lastPageId}}
                },
            };
            var client = new AmazonDynamoDBClient();
            var queryResponse = client.QueryAsync(queryRequest).Result;
            var items = new List<ClassificationModel>();
            foreach (var item in queryResponse.Items)
            {
                items.Add(JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(item).ToJson()));
            }
            response.Body = JsonConvert.SerializeObject(items);
        }
    }
}
