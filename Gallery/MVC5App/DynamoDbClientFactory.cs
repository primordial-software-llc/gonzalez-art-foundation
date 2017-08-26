using System;
using System.Collections.Generic;
using System.Linq;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace MVC5App
{
    public class DynamoDbClientFactory
    {
        private const string ARTIST_NAME_INDEX = "ArtistNameIndex";

        public string Create()
        {
            AWSCredentials instanceCredentials = new InstanceProfileAWSCredentials();
            var client = new AmazonDynamoDBClient(instanceCredentials, RegionEndpoint.USEast1);

            try
            {
                return FindAllForExactArtist(client, "jean-leon gerome").ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
        
        public int FindAllForExactArtist(AmazonDynamoDBClient client, string artist)
        {
            var queryRequest = new QueryRequest("ImageClassification");
            queryRequest.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":artist", new AttributeValue { S = artist } }
            };
            queryRequest.KeyConditionExpression = "artist = :artist";
            queryRequest.IndexName = ARTIST_NAME_INDEX;

            QueryResponse queryResponse = null;

            var allMatches = new List<Dictionary<string, AttributeValue>>();
            do
            {
                if (queryResponse != null)
                {
                    queryRequest.ExclusiveStartKey = queryResponse.LastEvaluatedKey;
                }
                queryResponse = client.Query(queryRequest);

                if (queryResponse.Items.Any())
                {
                    allMatches.AddRange(queryResponse.Items);
                }
            } while (queryResponse.LastEvaluatedKey.Any());

            return allMatches.Count;
        }


    }
}