using System;
using System.Linq;
using Amazon.DynamoDBv2;
using IndexBackend;
using IndexBackend.DataAccess;
using IndexBackend.Indexing;
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class ImageClassificationAccessTests
    {

        private readonly AmazonDynamoDBClient client = new AwsClientFactory().CreateDynamoDbClient();

        [Test]
        public void Test_Find_All_For_Exact_Artist()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.FindAllForExactArtist("Jean-Leon Gerome");
            Assert.AreEqual(244, results.Count);
        }

        [Test]
        public void Test_Find_All_For_Like_Artist()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.FindAllForLikeArtist("Jean-Leon Gerome");
            Assert.AreEqual(249, results.Count);
        }

        [Test]
        public void Test_Find_By_Label()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.FindByLabel("Ancient Egypt");
            Assert.AreEqual(2871, results.Count);
            results = results
                .Where(
                    x => x.LabelsAndConfidence.Any(y =>
                        y.ToLower().StartsWith("ancient egypt: 99", StringComparison.OrdinalIgnoreCase))
                )
                .ToList();
            foreach (var result in results)
            {
                foreach (var resultLabel in result.LabelsAndConfidence)
                {
                    Console.WriteLine(resultLabel);
                }
                Console.WriteLine(result.S3Path);
            }
            Console.WriteLine(results.Count);
            Assert.GreaterOrEqual(results.Count, 2);
        }

        [Test]
        public void Test_Scan()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.Scan(0, new TheAthenaeumIndexer().Source, 10);
            Assert.AreEqual(10, results.Count);
            Assert.AreEqual(33, results.First().PageId);
            Assert.AreEqual(42, results.Last().PageId);
            var results2 = dataAccess.Scan(results.Last().PageId, new TheAthenaeumIndexer().Source, 10);
            Assert.AreEqual(43, results2.First().PageId);
        }
    }
}
