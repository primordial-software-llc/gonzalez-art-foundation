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
            var regions = RegionEndpoint.EnumerableAllRegions
                .Where(x => x != RegionEndpoint.USGovCloudWest1 &&
                            x != RegionEndpoint.CNNorth1).ToList();

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
            Console.WriteLine(sw.Elapsed.TotalSeconds); // 85 seconds.
            // 85 * 435 = 10 hours to process all the ads! Now that's exagerated, because there aren't 150,00 ads there are only about 35-45,000. Los angeles is one of the biggest regions and most don't have 350 on the first page let alone have the first page filled by a single calendar day.

            // Still though let's say that's the case.
            // That means there will be 10 total hours.
            // Now let's split that up by 14 for each regoin and that now becomes.
            // 85 * (435/14) = 44 minutes.
            // Not bad. I can index every backpage ad in the united states every 45 minutes!
            // Now we are talking.
            // But I need two things.
            // First is an sqs queue, then my lambda symphony.

            // You see the process needs to be triggered by a master overseer who will take the 435 second level links and place them gently into an SQS queue.
            // Then the lambda functions are on a trigger to check for an item in the SQS queue.
            // When an item is found, it just runs this process here.
            // So two takeaways.

            // 1. Need an SQS queue so I can coodinate lambda functions around the globe.
            // 2. Need the ability to deploy a piece of code to lambda functions around the globe.
            
            // Once I have all of this I can start to do the processing with gate.
            // To start, I will ad the processing fields and a flag for "hasProcessedByGate" in dynamodb.
            // Then just do table scans for anything that hasn't been processed.
            // I might just have to scan once a night.
            // It shouldn't be a big deal.
            // What I really want is that god damn phone number field.
            // I mean think about it, I'll be collecting 30,000+ phone numbers per day.
            // My mind goes wild just thinking about the possibilities and making use of them.
            // There is so much potential. Alright get this.
            // Backpage Ad Phone Number -> Twilio -> ChatBot
            // Turn the tables on the advertisers pushing their smut.
        }
    }
}
