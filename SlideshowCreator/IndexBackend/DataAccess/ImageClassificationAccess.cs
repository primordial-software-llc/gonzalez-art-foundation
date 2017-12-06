using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GalleryBackend.Model;
using IndexBackend.DataAccess.ModelConversions;

namespace IndexBackend.DataAccess
{
    public class ImageClassificationAccess
    {
        public const string TABLE_IMAGE_CLASSIFICATION = "ImageClassificationV2";
        public const string ARTIST_NAME_INDEX = "ArtistNameIndex";

        private AmazonDynamoDBClient Client { get; }

        public ImageClassificationAccess(AmazonDynamoDBClient client)
        {
            Client = client;
        }

        public List<ClassificationModel> Scan(int lastPageId, string source)
        {
            var queryRequest = new QueryRequest(TABLE_IMAGE_CLASSIFICATION)
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":sources", new AttributeValue {S = source}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#source", "source"}
                },
                KeyConditionExpression = "#source = :sources"
            };
            queryRequest.ExclusiveStartKey = new Dictionary<string, AttributeValue>
            {
                {"source", new AttributeValue {S = source}},
                {"pageId", new AttributeValue {N = lastPageId.ToString()}}
            };

            var response = Client.Query(queryRequest);

            var typedResponse = new ClassificationConversion().ConvertToPoco(response.Items);
            return typedResponse;
        }

        public List<ClassificationModel> FindAllForExactArtist( string artist)
        {
            artist = artist.ToLower();

            var queryRequest = new QueryRequest(TABLE_IMAGE_CLASSIFICATION)
            {
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":artist", new AttributeValue {S = artist}}
                },
                KeyConditionExpression = "artist = :artist",
                IndexName = ARTIST_NAME_INDEX
            };

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

            var typedResponse = new ClassificationConversion().ConvertToPoco(allMatches);
            return typedResponse;
        }

        public List<ClassificationModel> FindAllForLikeArtist(string artist)
        {
            artist = artist.ToLower();

            var scanRequest = new ScanRequest(TABLE_IMAGE_CLASSIFICATION)
            {
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":artist", new AttributeValue {S = artist}}
                },
                FilterExpression = "contains(artist, :artist)",
                IndexName = ARTIST_NAME_INDEX
            };

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

            var typedResponse = new ClassificationConversion().ConvertToPoco(allMatches);
            return typedResponse;
        }
    }
}