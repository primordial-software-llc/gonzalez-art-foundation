using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            var items = FindAllForLikeArtist(client, artist, source, maxResults);
            response.Body = JsonConvert.SerializeObject(items);
        }

        public List<ClassificationModel> FindAllForLikeArtist(DatabaseClient<ClassificationModel> client, string artist, string source, int maxResults)
        {
            var allMatches = new ConcurrentBag<ClassificationModel>();
            var concurrency = 32;
            Parallel.For(0, concurrency, new ParallelOptions { MaxDegreeOfParallelism = concurrency }, threadCt =>
            {
                foreach (var match in FindAllForLikeArtistSingleThreaded(client, artist, source, threadCt, concurrency, maxResults))
                {
                    allMatches.Add(match);
                }
            });
            var results = allMatches
                .OrderBy(x => x.Artist)
                .ThenBy(x => x.Source)
                .ToList();
            while (maxResults > 0 && results.Count > maxResults)
            {
                results.RemoveAt(results.Count - 1);
            }
            return results;
        }

        public List<ClassificationModel> FindAllForLikeArtistSingleThreaded(
            DatabaseClient<ClassificationModel> client,
            string artist,
            string source,
            int segment,
            int totalSegments,
            int limit)
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
                IndexName = ClassificationModel.ARTIST_NAME_INDEX,
                Segment = segment,
                TotalSegments = totalSegments
            };
            return client.ScanAll(scanRequest, limit);
        }
    }
}
