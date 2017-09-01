using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SlideshowCreator.DataAccess;

namespace SlideshowCreator.Classification
{
    class ClassificationPersistenceExecutor
    {

        public const int CONCURRENCY = 5;

        [OneTimeSetUp]
        public void Setup_All_Tests_Once_And_Only_Once()
        {
            ServicePointManager.DefaultConnectionLimit = CONCURRENCY;
        }

        [Test]
        public void New_Table()
        {
            var client = new DynamoDbClientFactory().Create();

            var tableFactory = new DynamoDbTableFactory();
            var request = tableFactory.GetTableDefinition();
            tableFactory.CreateTable(request, client);
        }

        [Test]
        public void Create_Artist_Name_Index_On_New_Table()
        {
            var client = new DynamoDbClientFactory().Create();
            var tableFactory = new DynamoDbTableFactory();

            tableFactory.AddArtistNameGlobalSecondaryIndex(client, DynamoDbTableFactory.IMAGE_CLASSIFICATION_V2);
        }

        /// <summary>
        /// I have some work to do re-indexing.
        /// </summary>
        [Test]
        public void Check_Counts()
        {
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var tableDescription = client.DescribeTable(request.TableName);
            Console.WriteLine($"{request.TableName} item count: {tableDescription.Table.ItemCount}");
            Assert.AreEqual(288100, tableDescription.Table.ItemCount);

            foreach (var gsi in tableDescription.Table.GlobalSecondaryIndexes)
            {
                Console.WriteLine($"{gsi.IndexName}: " + gsi.ItemCount);
            }
        }

        [Test]
        public void Move_From_Table_V1_To_Table_V2()
        {
            AmazonDynamoDBClient client = new DynamoDbClientFactory().Create();

            Console.WriteLine("ImageClassification count initial: " + client.DescribeTable("ImageClassification").Table.ItemCount);
            Console.WriteLine($"{DynamoDbTableFactory.IMAGE_CLASSIFICATION_V2} count initial: " + client.DescribeTable(DynamoDbTableFactory.IMAGE_CLASSIFICATION_V2).Table.ItemCount);

            var scanRequest = new ScanRequest
            {
                TableName = "ImageClassification"
            };

            ScanResponse scanResponse = client.Scan(scanRequest);

            while (scanResponse.LastEvaluatedKey.Any())
            {
                scanResponse = client.Scan(scanRequest);
                
                List<ClassificationModel> pocoItems = new ClassificationConversion().ConvertToPoco(scanResponse.Items);
                Console.WriteLine(pocoItems.First().PageId);
                foreach (var pocoItem in pocoItems)
                {
                    pocoItem.Source = ClassificationConversion.THE_ATHENAEUM;
                }

                var pocosBatched = DynamoDbInsert.Batch(pocoItems);

                var parallelism = new ParallelOptions {MaxDegreeOfParallelism = CONCURRENCY};
                Parallel.ForEach(pocosBatched, parallelism, pocoBatch =>
                {
                    Dictionary<string, List<WriteRequest>> pocoBatchWrite =
                        DynamoDbInsert.GetBatchInserts(pocoBatch);
                    var batchWriteResponse = client.BatchWriteItem(pocoBatchWrite);

                    if (batchWriteResponse.UnprocessedItems.Any())
                    {
                        throw new Exception("Abort - Unprocessed Inserts");
                    }
                    var batchDeletes = DynamoDbDelete.GetBatchDeletes(pocoBatch, "ImageClassification");
                    client.BatchWriteItem(batchDeletes);
                });

                scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
            }

            Console.WriteLine("ImageClassification count final: " + client.DescribeTable("ImageClassification").Table.ItemCount);
            Console.WriteLine($"{DynamoDbTableFactory.IMAGE_CLASSIFICATION_V2} count final: " + client.DescribeTable(DynamoDbTableFactory.IMAGE_CLASSIFICATION_V2).Table.ItemCount);
        }
        
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
