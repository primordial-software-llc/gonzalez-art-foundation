using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GalleryBackend.Classification;
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

        /// <summary>
        /// I have some work to do re-indexing.
        /// </summary>
        [Test]
        public void Check_Counts()
        {
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var tableDescription = client.DescribeTable(request.TableName);
            Console.WriteLine($"{request.TableName} item count: {tableDescription.Table.ItemCount}");
            Assert.AreEqual(270858, tableDescription.Table.ItemCount); // I'll see if tonight or immediately after the web app I reindex. This is a lot now.

            foreach (var gsi in tableDescription.Table.GlobalSecondaryIndexes)
            {
                Console.WriteLine($"{gsi.IndexName}: " + gsi.ItemCount);
            }
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

    }
}
