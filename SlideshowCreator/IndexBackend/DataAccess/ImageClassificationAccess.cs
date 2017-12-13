using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using GalleryBackend.Model;
using IndexBackend.Indexing;

namespace IndexBackend.DataAccess
{
    public class ImageClassificationAccess
    {
        public const string ARTIST_NAME_INDEX = "ArtistNameIndex";

        private IAmazonDynamoDB Client { get; }

        public ImageClassificationAccess(IAmazonDynamoDB client)
        {
            Client = client;
        }

        public List<ClassificationModel> Scan(int? lastPageId, string source, int? limit = null)
        {
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
                    {"pageId", new AttributeValue {N = lastPageId.GetValueOrDefault().ToString()}}
                },
            };
            if (limit.HasValue)
            {
                queryRequest.Limit = limit.GetValueOrDefault();
            }
            var response = Client.Query(queryRequest);
            return Conversion<ClassificationModel>.ConvertToPoco(response.Items);
        }

        public List<ClassificationModel> FindAllForExactArtist(string artist, string source)
        {
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
                KeyConditionExpression = "artist = :artist",
                IndexName = ARTIST_NAME_INDEX,
                FilterExpression = "#source = :source"
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

            var typedResponse = Conversion<ClassificationModel>.ConvertToPoco(allMatches);
            return typedResponse;
        }

        public List<ClassificationModel> FindAllForLikeArtist(string artist, string source)
        {
            artist = artist.ToLower();

            var scanRequest = new ScanRequest(new ClassificationModel().GetTable())
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
                FilterExpression = "contains(artist, :artist) AND #source = :source",
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

            var typedResponse = Conversion<ClassificationModel>.ConvertToPoco(allMatches);
            return typedResponse;
        }

        public List<ImageLabel> FindByLabel(string label)
        {
            label = (label ?? string.Empty).ToLower();
            var scanRequest = new ScanRequest(new ImageLabel().GetTable())
            {
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":label", new AttributeValue {S = label}}
                },
                FilterExpression = "contains(normalizedLabels, :label)"
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
            return Conversion<ImageLabel>.ConvertToPoco(allMatches);
        }

        public ImageLabel GetLabel(int pageId)
        {
            var request = new QueryRequest(new ImageLabel().GetTable())
            {
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":source", new AttributeValue {S = new NationalGalleryOfArtIndexer().Source}},
                    {":pageId", new AttributeValue {N = pageId.ToString()}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#s", "source" }
                },
                KeyConditionExpression = "#s = :source AND pageId = :pageId",
                IndexName = "source-pageId-index"
            };
            QueryResponse response = null;
            var allMatches = new List<Dictionary<string, AttributeValue>>();
            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }
                response = Client.Query(request);
                if (response.Items.Any())
                {
                    allMatches.AddRange(response.Items);
                }
            } while (response.LastEvaluatedKey.Any());
            return Conversion<ImageLabel>.ConvertToPoco(allMatches.FirstOrDefault());
        }
    }
}