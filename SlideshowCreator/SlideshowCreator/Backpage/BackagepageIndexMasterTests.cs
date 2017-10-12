using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Amazon;
using IndexBackend;
using IndexBackend.DataAccess.ModelConversions;
using NUnit.Framework;
using SlideshowCreator.AwsAccess;
using SlideshowCreator.LambdaSymphony;

namespace SlideshowCreator.Backpage
{
    class BackagepageIndexMasterTests
    {
        private readonly BackpageCrawler backpageCrawler = new BackpageCrawler();

        [OneTimeSetUp]
        public void Setup()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
        }

        /// <summary>
        /// Process is pretty straightforward, now we need to multiply by 35,000 and deal with rate limiting.
        /// That's called reality.
        /// And if I want to distribute the work with Lambda, I need all functions to finish within 5 minutes.
        /// Or they run again or skip, and if they run again it's another hit by the rate limiter.
        /// </summary>
        [Test]
        public void Index_Every_Page_With_Content_On_Us_Backpage()
        {
            var sw = new Stopwatch();
            sw.Start();

            var regions = BackpageLambdaConfig.Regions;
            Console.WriteLine($"Distributing work into {regions.Count} AWS regions");
            foreach (RegionEndpoint region in regions)
            {
                Console.WriteLine(region.DisplayName + " - " + region.SystemName);
            }

            Uri firstLevelGeographicLink = BackpageCrawler.UnitedStatesHomePage;
            Console.WriteLine("Getting links from: " + firstLevelGeographicLink.AbsoluteUri);
            Dictionary<string, List<Uri>> linkDictionary =
                backpageCrawler.GetGeographicLocationLinks(firstLevelGeographicLink);

            var secondLevelLinks = linkDictionary.SelectMany(x => x.Value).ToList();
            Console.WriteLine($"Total second level links: {secondLevelLinks.Count}");

            int secondLevelLinkBatchSize = secondLevelLinks.Count / regions.Count;
            if (secondLevelLinks.Count % regions.Count > 0
            ) // Has remaineder, add one to batch size, can always go slightly larger and just leave a smaller batch for one region. This isn't exactly the problem I want to solve right now to even the distribution perfectly.
            {
                secondLevelLinkBatchSize += 1;
            }
            Console.WriteLine("Second level link batch size: " + secondLevelLinkBatchSize);
            Assert.IsTrue(secondLevelLinkBatchSize * regions.Count >= secondLevelLinks.Count);

            List<List<Uri>> secondLevelLinkBatches = Batcher.Batch(secondLevelLinkBatchSize, secondLevelLinks);
            Assert.AreEqual(14, secondLevelLinkBatches.Count);
            Assert.AreEqual(32, secondLevelLinkBatches.First().Count);
            Assert.AreEqual(19,
                secondLevelLinkBatches.Last()
                    .Count); // Last batch is slightly smaller from handling the remainder non-elegantly.

            // Now here's the question, how long will it take to run through a batch?
            // 32x however long it takes to pull each ad's content from one second level link.
            // Now how long does it take to pull each ad's content from one second level link?
            // Let's find out. I hope less than 5 minutes/300 seconds for a lambda function run.
            // If not, then it means we need two levels of distribution, in which case I estimate this taking 2 weeks to completion vs 1. Just to pull the ads with a lambda symphony.

            //Uri secondLevelGeographicLink = linkDictionary.Values.First().First();
            Uri secondLevelGeographicLink =
                secondLevelLinks.First(x => x.AbsoluteUri.StartsWith("http://losangeles.backpage.com"));
            var adLinksInGeographicRegion = backpageCrawler.GetAdLinksFromSection(secondLevelGeographicLink,
                BackpageCrawler.WOMEN_SEEKING_MEN_SECTION);
            Console.WriteLine($"Getting {adLinksInGeographicRegion.Count} ads from: " +
                              secondLevelGeographicLink.AbsoluteUri);

            foreach (var ad in adLinksInGeographicRegion)
            {
                ad.Body = backpageCrawler.GetAdBody(ad.Uri);
            }
            Console.WriteLine(sw.Elapsed.TotalSeconds); // 83 seconds.
            var client = new AwsClientFactory().CreateDynamoDbClient();
            var adBatches = DynamoDbInsert.Batch(adLinksInGeographicRegion);
            foreach (var adBatch in adBatches)
            {
                Console.WriteLine($"Inserting batch - sample: {adBatch.First().Source} - {adBatch.First().Uri.AbsoluteUri}");
                var adBatchInsert = DynamoDbInsert.GetBatchInserts(adBatch, new BackpageAdModelConversion());
                var batchInsertResult = client.BatchWriteItem(adBatchInsert);
                Assert.AreEqual(0, batchInsertResult.UnprocessedItems.Count); // There is code for this crap, but I don't want that complexity.
            }
            Console.WriteLine(sw.Elapsed.TotalSeconds);
        }
    }
}
