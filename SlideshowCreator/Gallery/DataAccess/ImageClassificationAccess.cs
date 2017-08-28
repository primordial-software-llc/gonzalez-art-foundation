using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace MVC5App.DataAccess
{
    public class ImageClassificationAccess
    {
        private const string TABLE_NAME = "ImageClassification";
        private const string ARTIST_NAME_INDEX = "ArtistNameIndex";
        private AmazonDynamoDBClient Client { get; }

        public ImageClassificationAccess(AmazonDynamoDBClient client)
        {
            Client = client;
        }

        public int FindAllForExactArtist( string artist)
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
                queryResponse = Client.Query(queryRequest);

                if (queryResponse.Items.Any())
                {
                    allMatches.AddRange(queryResponse.Items);
                }
            } while (queryResponse.LastEvaluatedKey.Any());

            return allMatches.Count;
        }

        public int FindAllForLikeArtist(string artist)
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
                scanResponse = Client.Scan(scanRequest);

                if (scanResponse.Items.Any())
                {
                    allMatches.AddRange(scanResponse.Items);
                }
            } while (scanResponse.LastEvaluatedKey.Any());

            return allMatches.Count;
        }
    }
}