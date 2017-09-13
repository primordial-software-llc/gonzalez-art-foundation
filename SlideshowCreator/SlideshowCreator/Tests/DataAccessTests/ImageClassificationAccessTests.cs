using System;
using System.Linq;
using Amazon.DynamoDBv2;
using IndexBackend;
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class ImageClassificationAccessTests
    {

        private readonly AmazonDynamoDBClient client = new DynamoDbClientFactory().Create();

        [Test]
        public void Test_Find_All_For_Exact_Artist()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.FindAllForExactArtist("Jean-Leon Gerome");
            Console.WriteLine(results);

            Assert.AreEqual(233, results.Count); // Should be 244. According to the site.
        }

        [Test]
        public void Test_Find_All_For_Like_Artist()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.FindAllForLikeArtist("Jean-Leon Gerome");
            Assert.AreEqual(237, results.Count); // Should be 249. According to the site there are 244 Jean-Leon Gerome and 5 Jean-Leon Gerome Ferris.
        }

        [Test]
        public void Test_Scan()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.Scan(0);
            Assert.AreEqual(7350, results.Count);
            Assert.AreEqual(33, results.First().PageId);
            Assert.AreEqual(7891, results.Last().PageId);

            var results2 = dataAccess.Scan(results.Last().PageId);
            Assert.AreEqual(7055, results2.Count);
            Assert.AreEqual(7892, results2.First().PageId);
            Assert.AreEqual(15418, results2.Last().PageId);
        }
    }
}
