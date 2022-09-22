using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using ArtApi.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            var artists = new ConcurrentDictionary<string, string>();
            var request = new ScanRequest(sourceClassificationTable);
            ScanResponse response = null;
            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }
                response = DynamoDbClient.ScanAsync(request).Result;
                Parallel.ForEach(response.Items, item =>
                {
                    var model = JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(item).ToJson());
                    if (string.IsNullOrWhiteSpace(model.Artist))
                    {
                        Console.WriteLine("No artist");
                    }
                    if (!string.IsNullOrWhiteSpace(model.Artist) &&
                        !artists.ContainsKey(model.Artist))
                    {
                        artists.TryAdd(model.Artist, model.OriginalArtist);
                        var artistModel = new ArtistModel
                        {
                            Artist = model.Artist,
                            OriginalArtist = model.OriginalArtist
                        };
                        var json = JsonConvert.SerializeObject(artistModel, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        DynamoDbClient.PutItemAsync(
                            destinationArtistTable,
                            Document.FromJson(json).ToAttributeMap()
                        ).Wait();
                    }
                });
            } while (response.LastEvaluatedKey.Any());
        }

    }
}
