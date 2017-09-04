using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GalleryBackend.DataAccess;
using NUnit.Framework;
using SlideshowCreator.Classification;
using SlideshowCreator.DataAccess;
using SlideshowCreator.InfrastructureAsCode;

namespace SlideshowCreator.Scripts
{
    class ClassificationScript
    {

        public const int CONCURRENCY = 5;
        private readonly AmazonDynamoDBClient client = new DynamoDbClientFactory().Create();

        [OneTimeSetUp]
        public void Setup_All_Tests_Once_And_Only_Once()
        {
            ServicePointManager.DefaultConnectionLimit = CONCURRENCY;
        }

        //[Test]
        public void New_Table()
        {
            var client = new DynamoDbClientFactory().Create();

            var tableFactory = new DynamoDbTableFactory();
            var request = tableFactory.GetTableDefinition();
            tableFactory.CreateTable(request, client);

            // This should work fine, but I've never run these one-after-the-other.
            // However, I wait for the table to be active at the end of table creation.
            tableFactory.AddArtistNameGlobalSecondaryIndex(client, ImageClassificationAccess.IMAGE_CLASSIFICATION_V2);
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
            Assert.AreEqual(270858, tableDescription.Table.ItemCount); // I'll see if tonight or immediately after the web app I reindex. This is a lot now.

            foreach (var gsi in tableDescription.Table.GlobalSecondaryIndexes)
            {
                Console.WriteLine($"{gsi.IndexName}: " + gsi.ItemCount);
            }
        }

        //[Test]
        public void Move_From_Table_V1_To_Table_V2()
        {
            AmazonDynamoDBClient client = new DynamoDbClientFactory().Create();

            Console.WriteLine("ImageClassification count initial: " + client.DescribeTable("ImageClassification").Table.ItemCount);
            Console.WriteLine($"{ImageClassificationAccess.IMAGE_CLASSIFICATION_V2} count initial: " + client.DescribeTable(ImageClassificationAccess.IMAGE_CLASSIFICATION_V2).Table.ItemCount);

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
                    pocoItem.Source = ImageClassificationAccess.THE_ATHENAEUM;
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
            Console.WriteLine($"{ImageClassificationAccess.IMAGE_CLASSIFICATION_V2} count final: " + client.DescribeTable(ImageClassificationAccess.IMAGE_CLASSIFICATION_V2).Table.ItemCount);
        }
        
        [Test]
        public void Reclassify_Jean_Leon_Gerome_Sample()
        {
            var privateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");
            var transientClassifier = new TransientClassification(privateConfig, client, ImageClassificationAccess.IMAGE_CLASSIFICATION_V2);
            var classification = transientClassifier.ReclassifyTheAthenaeumTransiently(15886);

            Assert.AreEqual("http://www.the-athenaeum.org", classification.Source);
            Assert.AreEqual(15886, classification.PageId);
            Assert.AreEqual(153045, classification.ImageId);
            Assert.AreEqual("The Slave Market", classification.Name); 
            Assert.AreEqual("jean-leon gerome", classification.Artist);
            Assert.AreEqual("Jean-Léon Gérôme", classification.OriginalArtist);
            Assert.AreEqual("1866", classification.Date);
        }

        [Test]
        public void Test_Find_All_For_Exact_Artist()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.FindAllForExactArtist("Jean-Leon Gerome");
            Console.WriteLine(results);

            Assert.AreEqual(233, results); // Should be 244. According to the site.
        }

        // Slower than I'd like at 7 seconds, but there are 280,000 records.
        // This is fine for personal usage.
        // Elastic search is needed to solve this.
        // That's designed for full-text search.
        // The only possible work-around is a new index on source and artist name as the range key.
        // I still don't know if this will work, there is a begins_with and a between.
        // I'm not sure if between is valid for character strings, begins with could work, but even that is limited.
        // For now it's a non-issue. I can wait 7 seconds if I want to do a search by artist name, then drill into the artist and
        // search by exact artist. Once I know the artist I don't have to do a contains anymore.
        // If I want to be really good, I can put the LIKE operation behind exact matches when there are no results.
        [Test]
        public void Test_Find_All_For_Like_Artist()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.FindAllForLikeArtist("Jean-Leon Gerome");
            Assert.AreEqual(237, results); // Should be 249. According to the site there are 244 Jean-Leon Gerome and 5 Jean-Leon Gerome Ferris.
        }

    }
}
