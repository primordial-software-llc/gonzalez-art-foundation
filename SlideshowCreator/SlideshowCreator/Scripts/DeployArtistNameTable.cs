using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.Indexing;
using NUnit.Framework;
using SlideshowCreator.InfrastructureAsCode;

namespace SlideshowCreator.Scripts
{
    class DeployArtistNameTable
    {

        [Test]
        public void A_Deploy_Artist_Name_Table()
        {
            var client = GalleryAwsCredentialsFactory.DbClient;
            var tableFactory = new DynamoDbTableFactory(client);
            var request = new CreateTableRequest
            {
                TableName = new ArtistModel().GetTable(),
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = ArtistModel.ARTIST,
                        KeyType = "HASH"
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = ArtistModel.ARTIST,
                        AttributeType = "S"
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };
            tableFactory.CreateTable(request);
        }

        [Test]
        public void B_Fill_Artist_Table()
        {
            var client = GalleryAwsCredentialsFactory.DbClient;
            var toolsClient = new DynamoDbClient<ArtistModel>(client, new ConsoleLogging());

            var scanRequest = new QueryRequest(new ClassificationModel().GetTable());
            QueryResponse scanResponse = null;

            HashSet<string> artistWorks = new HashSet<string>();
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanRequest.ProjectionExpression = $"{ArtistModel.ARTIST}";
                scanRequest.KeyConditions = new Dictionary<string, Condition>
                {
                    {
                        "source",
                        new Condition
                        {
                            ComparisonOperator = "EQ",
                            AttributeValueList = new List<AttributeValue>
                            {
                                new AttributeValue {S = new TheAthenaeumIndexer().Source}
                            }
                        }
                    }
                };
                scanResponse = client.Query(scanRequest);
                var images = Conversion<ClassificationModel>.ConvertToPoco(scanResponse.Items)
                    .ToList();
                foreach (var image in images)
                {
                    artistWorks.Add(image.Artist);
                }
                
            } while (scanResponse.LastEvaluatedKey.Any());

            var toInsert = artistWorks
                .Where(x => !string.IsNullOrWhiteSpace(x) && x.Trim().Length > 0)
                .Select(x => new ArtistModel {Artist = x.Trim()})
                .OrderBy(x => x.Artist)
                .ToList();

            var batches = Batcher.Batch(25, toInsert);
            foreach (var batch in batches)
            {
                var batchCopy = batch.ToList();
                while (batchCopy.Any())
                {
                    batchCopy = toolsClient.Insert(batchCopy).Result;
                }
            }
        }
        
    }
}
