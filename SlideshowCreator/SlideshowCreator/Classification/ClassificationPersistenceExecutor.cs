using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;

namespace SlideshowCreator.Classification
{
    class ClassificationPersistenceExecutor
    {
        private const string ARTIST_NAME_INDEX = "ArtistNameIndex";

        //[Test]
        public void Add_GSI()
        {
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var artistNameIndexRequest = new GlobalSecondaryIndexUpdate();
            artistNameIndexRequest.Create = new CreateGlobalSecondaryIndexAction();
            artistNameIndexRequest.Create.IndexName = ARTIST_NAME_INDEX;
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

        /// <summary>
        /// I have some work to do re-indexing.
        /// </summary>
        //[Test]
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
            Assert.AreEqual("jean-leon gerome", classification.Artist);
            Assert.AreEqual("Jean-Léon Gérôme", classification.OriginalArtist);
            Assert.AreEqual("1866", classification.Date);
        }

    }
}
