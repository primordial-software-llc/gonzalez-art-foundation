using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;

namespace SlideshowCreator.Classification
{
    class ClassificationPersistenceExecutor
    {

        [Test]
        public void Add_GSI()
        {
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var artistNameIndexRequest = new GlobalSecondaryIndexUpdate();
            artistNameIndexRequest.Create = new CreateGlobalSecondaryIndexAction();
            artistNameIndexRequest.Create.IndexName = "ArtistNameIndex";
            artistNameIndexRequest.Create.ProvisionedThroughput = new ProvisionedThroughput(25, 5);
            artistNameIndexRequest.Create.Projection = new Projection { ProjectionType = ProjectionType.ALL };
            artistNameIndexRequest.Create.KeySchema = new List<KeySchemaElement> {
                new KeySchemaElement { AttributeName = "artist", KeyType = "HASH"},
                new KeySchemaElement {AttributeName = "name", KeyType = "RANGE"}
            };

            var updateTableRequest = new UpdateTableRequest();
            updateTableRequest.TableName = request.TableName;
            updateTableRequest.GlobalSecondaryIndexUpdates.Add(artistNameIndexRequest);
            updateTableRequest.AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "artist",
                    AttributeType = "S"
                },
                new AttributeDefinition
                {
                    AttributeName = "name",
                    AttributeType = "S"
                }
            };

            client.UpdateTable(updateTableRequest);
        }

        // All of this became obsolete with transient classification.
        // The table creation itslef is obsolete, except for "Phoenix" scenarios.

        /// <summary>
        /// I have some work to do re-indexing.
        /// </summary>
        [Test]
        public void Check_Count()
        {
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var tableDescription = client.DescribeTable(request.TableName);
            Console.WriteLine(tableDescription.Table.ItemCount);

            Assert.AreEqual(288100, tableDescription.Table.ItemCount);
        }
        
        /// <summary>
        /// I will need to deal with name diacritics when searching by name is a supported use case.
        /// Diacritics become very imporant considering the breadth of this work.
        /// </summary>
        [Test]
        public void Reclassify_Jean_Leon_Gerome_Sample()
        {
            var privateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");
            var transientClassifier = new TransientClassification(privateConfig);
            var classification = transientClassifier.ReclassifyTransiently(15886);

            Assert.AreEqual(15886, classification.PageId);
            Assert.AreEqual(153045, classification.ImageId);
            Assert.AreEqual("The Slave Market", classification.Name); 
            Assert.AreEqual("Jean-Leon Gerome", classification.Artist);
            Assert.AreEqual("Jean-Léon Gérôme", classification.OriginalArtist);
            Assert.AreEqual("1866", classification.Date);
        }
        

        // Not bad 5.486 seconds to search 250,000+ records with a contains.
        [TestCase("Jean-Leon Gerome", 244)]
        public void Find_All_For_Artist(string artist, int expectedWorks)
        {
            var client = new DynamoDbClientFactory().Create(); // Lookup the old artists name.
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var scanRequest = new ScanRequest(request.TableName);
            scanRequest.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":artist", new AttributeValue { S = artist } }
            };
            scanRequest.FilterExpression = "contains(artist, :artist)";
            //scanRequest.FilterExpression = "artist = :artist";

            ScanResponse scanResponse = null;

            var allMatches = new List<Dictionary<string, AttributeValue>>();
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanResponse = client.Scan(scanRequest);

                if (scanResponse.Items.Any())
                {
                    allMatches.AddRange(scanResponse.Items);
                }
            } while (scanResponse.LastEvaluatedKey.Any());

            Assert.AreEqual(expectedWorks, allMatches.Count);
        }
        
    }
}
