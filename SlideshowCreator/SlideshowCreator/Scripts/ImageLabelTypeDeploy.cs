using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using NUnit.Framework;
using SlideshowCreator.InfrastructureAsCode;

namespace SlideshowCreator.Scripts
{
    class ImageLabelTypeDeploy
    {
        private readonly IAmazonDynamoDB dbClient = GalleryAwsCredentialsFactory.DbClient;

        //[Test]
        public void Run()
        {
            var tableFactory = new DynamoDbTableFactory(dbClient);
            var request = new CreateTableRequest
            {
                TableName = new ImageLabelType().GetTable(),
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "label",
                        KeyType = "HASH"
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = "label",
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

        //[Test]
        public void Populate_Image_Label_Types_From_Images()
        {
            var awsToolsClient = new DynamoDbClient<ImageLabelType>(dbClient, new ConsoleLogging());

            HashSet<string> labels = new HashSet<string>();
            var request = new ScanRequest(new ImageLabel().GetTable());
            ScanResponse response = null;
            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }
                response = dbClient.Scan(request);
                var labelBatch = Conversion<ImageLabel>
                    .ConvertToPoco(response.Items)
                    .Where(x => x.NormalizedLabels != null)
                    .SelectMany(x => x.NormalizedLabels)
                    .Distinct()
                    .ToList();
                foreach (var label in labelBatch)
                {
                    if (labels.Add(label))
                    {
                        awsToolsClient.Insert(new ImageLabelType
                        {
                            Label = label
                        }).Wait();
                    }
                }
            } while (response.LastEvaluatedKey.Any());
        }

        [Test]
        public void Count_Image_Label_Types()
        {
            var request = new ScanRequest(new ImageLabelType().GetTable());
            var all = new List<ImageLabelType>();
            ScanResponse response = null;
            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }
                response = dbClient.Scan(request);
                all.AddRange(Conversion<ImageLabelType>.ConvertToPoco(response.Items));
            } while (response.LastEvaluatedKey.Any());

            all = all.OrderBy(x => x.Label).ToList();
            Console.WriteLine(all.Count);
            foreach (var item in all)
            {
                Console.WriteLine(item.Label);
            }
        }

    }
}
