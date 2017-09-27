using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IndexBackend;
using NUnit.Framework;

namespace SlideshowCreator.Tests.Backpage
{
    class BackpageResponseTests
    {
        private readonly BackpageCrawler backpageCrawler = new BackpageCrawler();
        private Dictionary<string, List<Uri>> linkDictionary;

        [OneTimeSetUp]
        public void Setup()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            linkDictionary = backpageCrawler.GetGeographicLocationLinks(BackpageCrawler.UnitedStatesHomePage);

            Assert.AreEqual(51, linkDictionary.Count);
            Assert.IsTrue(linkDictionary.All(x => x.Value.Count > 0));
            Assert.AreEqual(435, linkDictionary.Values.Sum(x => x.Count));
        }

        [Test]
        public void True_Server_Is_Apache_And_Serves_Two_Requests_Then_Goes_To_Edgecast_Cdn_Cache_On_Third_Attempt()
        {
            var cofig = PrivateConfig.CreateFromPersonalJson();

            var url = cofig.SomeUrl + Guid.NewGuid();
            Console.WriteLine(url);

            HttpResponseMessage response = backpageCrawler.Client.GetAsync(url).Result;
            Assert.IsFalse(HttpHeaders.HasHeader(
                response.Headers, HttpHeaders.Names.X_CACHE, HttpHeaders.XCache.HIT));
            Assert.IsTrue(HttpHeaders.HasHeader(
                response.Headers, HttpHeaders.Names.SERVER, HttpHeaders.Server.APACHE));

            response = backpageCrawler.Client.GetAsync(url).Result;
            Assert.IsFalse(HttpHeaders.HasHeader(
                response.Headers, HttpHeaders.Names.X_CACHE, HttpHeaders.XCache.HIT));
            Assert.IsTrue(HttpHeaders.HasHeader(
                response.Headers, HttpHeaders.Names.SERVER, HttpHeaders.Server.APACHE));

            response = backpageCrawler.Client.GetAsync(url).Result;
            Assert.IsTrue(HttpHeaders.HasHeader(
                response.Headers, HttpHeaders.Names.X_CACHE, HttpHeaders.XCache.HIT));
            Assert.IsTrue(HttpHeaders.HasHeader(
                response.Headers, HttpHeaders.Names.SERVER, HttpHeaders.Server.ECS));
        }

        /// <summary>
        /// This only takes about 5 minutes. Not quite as fast as I wanted, but not too bad.
        /// I'm getting rate limited. Not sure if it's worth circumventing given the relative speed.
        /// It's probably more important to wait and deal with when getting the details.
        /// 
        /// This isn't perfect, because it doesn't get all for the day.
        /// However, the plan was to scan multiple times a day to see what's getting reported and removed.
        /// </summary>
        [Test]
        public void Get_All_AdLinksInUsOnFirstPageOfEachSection()
        {
            Assert.AreEqual(51, linkDictionary.Count);
            Assert.IsTrue(linkDictionary.All(x => x.Value.Count > 0));
            Assert.AreEqual(435, linkDictionary.Values.Sum(x => x.Count));

            List<Uri> usAdLinks = new List<Uri>();
            //var adLinksInGeographicRegion = backpageCrawler.GetAdLinksFromSection(linkDictionary.Values.First().First(), BackpageCrawler.WOMEN_SEEKING_MEN_SECTION, 0);

            foreach (KeyValuePair<string, List<Uri>> links in linkDictionary)
            {
                // Could possibly go in parallel two, but it's not safe yet. Needs more testing.
                // I trust this code here though.
                //var parallelOptions = new ParallelOptions();
                //parallelOptions.MaxDegreeOfParallelism = int.MaxValue; // Getting 502 errors with int.maxValue
                //Parallel.ForEach(links.Value, parallelOptions, geographicLink => // Links by state e.g. New York, California, Texas, etc.
                foreach (var geographicLink in links.Value)
                {
                    var adLinksInGeographicRegion = backpageCrawler.GetAdLinksFromSection(geographicLink, BackpageCrawler.WOMEN_SEEKING_MEN_SECTION);

                    foreach (var adLink in adLinksInGeographicRegion) // Links by city/region e.g. Manhattan, Los Angeles, Dallas
                    {
                        usAdLinks.Add(adLink);
                    }
                }//);
            }

            Console.WriteLine(usAdLinks.Count);
            foreach (var usAdLink in usAdLinks)
            {
                Console.WriteLine(usAdLink);
            }
        }

        [Test]
        public void Uri_Not_Too_Long_At_225_Guids()
        {
            var cofig = PrivateConfig.CreateFromPersonalJson();
            var url = cofig.SomeUrl + string.Join("", GetGuids(225));
            HttpResponseMessage response = backpageCrawler.Client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
        }

        [Test]
        public void Uri_Too_Long_At_226_Guids_414_Status_Code()
        {
            var cofig = PrivateConfig.CreateFromPersonalJson();
            var url = cofig.SomeUrl + string.Join("", GetGuids(226));
            HttpResponseMessage response = backpageCrawler.Client.GetAsync(url).Result;
            Assert.AreEqual(HttpStatusCode.RequestUriTooLong, response.StatusCode);
        }

        private List<Guid> GetGuids(int numberOfGuids)
        {
            var guids = new List<Guid>();
            for (var ct = 0; ct < numberOfGuids; ct++)
            {
                guids.Add(Guid.NewGuid());
            }
            return guids;
        }

    }
}
