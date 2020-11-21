using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private const int CONCURRENCY = 2;

        private IAmazonDynamoDB Client { get; }

        public ImageClassificationAccess(IAmazonDynamoDB client)
        {
            Client = client;
        }

        public List<ClassificationModelNew> Scan(int? lastPageId, string source, int? limit = null)
        {
            var queryRequest = new QueryRequest(new ClassificationModelNew().GetTable())
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
            return Conversion<ClassificationModelNew>.ConvertToPoco(response.Items);
        }



        public List<ImageLabel> FindByLabel(
            string label, string source)
        {
            var allMatches = new ConcurrentBag<ImageLabel>();

            Parallel.For(0, CONCURRENCY, new ParallelOptions { MaxDegreeOfParallelism = CONCURRENCY }, threadCt =>
            {
                foreach (var match in FindByLabelSingleThreaded(label, source, threadCt, CONCURRENCY))
                {
                    allMatches.Add(match);
                }
            });

            return allMatches.ToList();
        }

        public List<ImageLabel> FindByLabelSingleThreaded(string label, string source, int segment, int totalSegments)
        {
            label = (label ?? string.Empty).ToLower();
            var scanRequest = new ScanRequest(new ImageLabel().GetTable())
            {
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":label", new AttributeValue {S = label}},
                    {":source", new AttributeValue {S = source}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#source", "source" }
                },
                FilterExpression = "contains(normalizedLabels, :label) AND #source = :source",
                Segment = segment,
                TotalSegments = totalSegments
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