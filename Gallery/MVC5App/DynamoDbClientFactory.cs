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
        private const string TABLE_NAME = "ImageClassification";
        private const string ARTIST_NAME_INDEX = "ArtistNameIndex";

        public string Create()
        {
            AWSCredentials instanceCredentials = new InstanceProfileAWSCredentials();
            var client = new AmazonDynamoDBClient(instanceCredentials, RegionEndpoint.USEast1);

            try
            {
                string result = "Exact matches (expect 244): " + FindAllForExactArtist(client, "jean-leon gerome") +
                                " Like matches (expect 249): " + FindAllForLikeArtist(client, "jean-leon gerome");
                return result;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
        
        private int FindAllForExactArtist(AmazonDynamoDBClient client, string artist)
        {
            var queryRequest = new QueryRequest(TABLE_NAME);
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
        
        private int FindAllForLikeArtist(AmazonDynamoDBClient client, string artist)
        {
            var scanRequest = new ScanRequest(TABLE_NAME);
            scanRequest.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":artist", new AttributeValue { S = artist } }
            };
            scanRequest.FilterExpression = "contains(artist, :artist)";
            scanRequest.IndexName = ARTIST_NAME_INDEX;

            ScanResponse scanResponse = null;

            var allMatches = new List<Dictionary<string, AttributeValue>>();
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanResponse = client.Scan(scanRequest);

                if (scanResponse.Items.Any())
                {
                    allMatches.AddRange(scanResponse.Items);
                }
            } while (scanResponse.LastEvaluatedKey.Any());

            return allMatches.Count;
        }

    }
}