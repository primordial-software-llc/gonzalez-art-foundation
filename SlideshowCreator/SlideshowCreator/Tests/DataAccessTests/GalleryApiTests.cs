using System;
using System.Linq;
using System.Net;
using GalleryBackend;
using IndexBackend;
using IndexBackend.Indexing;
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class GalleryApiTests
    {
        private readonly PrivateConfig privateConfig = PrivateConfig.CreateFromPersonalJson();
        private GalleryClient client;

        [OneTimeSetUp]
        public void Authenticate()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            client = new GalleryClient("tgonzalez.net", privateConfig.GalleryUsername, privateConfig.GalleryPassword);
        }
        
        [Test]
        public void Exact_Artist()
        {
            var artist = "Jean-Leon Gerome";
            var results = client.SearchExactArtist(artist, new TheAthenaeumIndexer().Source);
            Assert.AreEqual(244, results.Count);
        }

        [Test]
        public void Like_Artist()
        {
            var artist = "Jean-Leon Gerome";
            var results = client.SearchLikeArtist(artist, new TheAthenaeumIndexer().Source);
            Assert.AreEqual(249, results.Count);
        }
        
        [Test]
        public void Scan()
        {
            var results = client.Scan(null, new TheAthenaeumIndexer().Source);
            Assert.AreEqual(7332, results.Count);
        }

        [Test]
        public void IP_Address()
        {
            var ipAddress = client.GetIPAddress();
            Console.WriteLine("IP received by web server expected to be from CDN: " + ipAddress.IP);
            Console.WriteLine("IP recevied by web server expected to be original: " + ipAddress.OriginalVisitorIPAddress);
            Assert.AreEqual(privateConfig.DecryptedIp, ipAddress.OriginalVisitorIPAddress);
        }

        [Test]
        public void Search_Labels()
        {
            var results = client.SearchLabel("Ancient Egypt");
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

    }
}
