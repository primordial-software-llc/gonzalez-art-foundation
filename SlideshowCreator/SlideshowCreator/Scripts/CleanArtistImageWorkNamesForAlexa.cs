using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.Indexing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Scripts
{
    class CleanArtistImageWorkNamesForAlexa
    {
            
        [Test]
        public void B_Fill_Artist_Table()
        {
            var client = GalleryAwsCredentialsFactory.DbClient;
            var toolsClient = new DynamoDbClient<ClassificationModel>(client, new ConsoleLogging());

            var path = @"C:\Users\peon\Desktop\projects\SlideshowCreator\SlideshowCreator\clean-artist-work-progress.json";
            var scanRequest = new QueryRequest(new ClassificationModel().GetTable());
            QueryResponse scanResponse = null;

            if (File.Exists(path))
            {
                var keyText = File.ReadAllText(path);
                var keyParsed = JsonConvert.DeserializeObject<Dictionary<string, AttributeValue>>(keyText);
                scanRequest.ExclusiveStartKey = Conversion<ClassificationModel>.ConvertToPoco(keyParsed).GetKey();
            }
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
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
                var toInsert = new List<ClassificationModel>();
                foreach (var image in images.Where(x => x.Name.Contains("<")))
                {
                    image.Name = HtmlToText.GetText(image.Name);
                    toInsert.Add(image);
                }
                var batches = Batcher.Batch(25, toInsert);
                foreach (var batch in batches)
                {
                    var batchCopy = batch.ToList();
                    while (batchCopy.Any())
                    {
                        batchCopy = toolsClient.Insert(batchCopy).Result;
                    }
                }
            } while (scanResponse.LastEvaluatedKey.Any());

        }

    }
}
