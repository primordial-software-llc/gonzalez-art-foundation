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

            Assert.AreEqual(238, results.Count); // Should be 244. According to the site.
        }

        [Test]
        public void Test_Find_All_For_Like_Artist()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.FindAllForLikeArtist("Jean-Leon Gerome");
            Assert.AreEqual(243, results.Count); // Should be 249. According to the site there are 244 Jean-Leon Gerome and 5 Jean-Leon Gerome Ferris.
        }

        [Test]
        public void Test_Scan()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.Scan(0, new TheAthenaeumIndexer().Source);
            Assert.AreEqual(7331, results.Count);
            Assert.AreEqual(33, results.First().PageId);
            Assert.AreEqual(7413, results.Last().PageId);

            var results2 = dataAccess.Scan(results.Last().PageId, new TheAthenaeumIndexer().Source);
            Assert.AreEqual(7111, results2.Count);
            Assert.AreEqual(7414, results2.First().PageId);
            Assert.AreEqual(14752, results2.Last().PageId);
        }
    }
}
