using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ArtApi.Model;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;

namespace IndexBackend.DataMaintenance
{
    public class RebuildArtistListTableProcess
    {
        private IAmazonDynamoDB DynamoDbClient { get; }

        public RebuildArtistListTableProcess(IAmazonDynamoDB dynamoDbClient)
        {
            DynamoDbClient = dynamoDbClient;
        }

        public void RebuildArtistListTable(string sourceClassificationTable, string destinationArtistTable)
        {
            ClearArtistListTable(destinationArtistTable);
            BuildArtistListTable(sourceClassificationTable, destinationArtistTable);
        }

        private void ClearArtistListTable(string destinationArtistTable)
        {
            var request = new ScanRequest(destinationArtistTable);
            ScanResponse response;
            do
            {
                response = DynamoDbClient.ScanAsync(request).Result;
                Parallel.ForEach(response.Items, item =>
                {
                    var model = JsonConvert.DeserializeObject<ArtistModel>(Document.FromAttributeMap(item).ToJson());
                    DynamoDbClient.DeleteItemAsync(destinationArtistTable, model.GetKey()).Wait();
                });
            } while (response.Items.Any());
        }

        private void BuildArtistListTable(string sourceClassificationTable, string destinationArtistTable)
        {
            var artists = new ConcurrentDictionary<string, ArtistModel>();
            var request = new ScanRequest(sourceClassificationTable);
            ScanResponse response = null;
            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }
                response = DynamoDbClient.ScanAsync(request).Result;
                var models = response
                    .Items                                                                  
                    .Select(x => JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(x).ToJson()));
                var modelsByArtist = models
                    .Where(x => !string.IsNullOrWhiteSpace(x.Artist))
                    .GroupBy(x => x.Artist);
                foreach (var worksOfArtByArtist in modelsByArtist)
                {
                    ArtistModel artistModel;
                    if (artists.ContainsKey(worksOfArtByArtist.Key))
                    {
                        artistModel = artists[worksOfArtByArtist.Key];
                    }
                    else
                    {
                        artistModel = new ArtistModel { Artist = worksOfArtByArtist.Key, OriginalArtist = worksOfArtByArtist.First().OriginalArtist };
                        artists.TryAdd(worksOfArtByArtist.Key, artistModel);
                    }
                    artistModel.NumberOfWorks += worksOfArtByArtist.Count();
                }
            } while (response.LastEvaluatedKey.Any());

            Parallel.ForEach(artists.Keys, artist =>
            {
                var artistModel = artists[artist];
                var json = JsonConvert.SerializeObject(artistModel, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                DynamoDbClient.PutItemAsync(
                    destinationArtistTable,
                    Document.FromJson(json).ToAttributeMap()
                ).Wait();
            });
        }

    }
}
