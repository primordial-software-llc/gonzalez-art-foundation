using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
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

        [Test]
        public void Get_Ads_On_2017_10_6_For_Los_Angeles()
        {
            var adLinksInGeographicRegion = backpageCrawler.GetAdLinksFromSection(
                new Uri("http://losangeles.backpage.com"), BackpageCrawler.WOMEN_SEEKING_MEN_SECTION, 0);
            Console.WriteLine(adLinksInGeographicRegion.Count);

            foreach (var link in adLinksInGeographicRegion)
            {
                Console.WriteLine(link);
            }
        }

        [Test]
        public void Get_All_AdLinksInUsOnFirstPageOfEachSection()
        {
            Assert.AreEqual(51, linkDictionary.Count);
            Assert.IsTrue(linkDictionary.All(x => x.Value.Count > 0));
            Assert.AreEqual(435, linkDictionary.Values.Sum(x => x.Count));

            List<Uri> usAdLinks = new List<Uri>();

            foreach (KeyValuePair<string, List<Uri>> links in linkDictionary)
            {
                foreach (var geographicLink in links.Value)
                {
                    var adLinksInGeographicRegion = backpageCrawler.GetAdLinksFromSection(geographicLink, BackpageCrawler.WOMEN_SEEKING_MEN_SECTION);

                    foreach (var adLink in adLinksInGeographicRegion)
                    {
                        usAdLinks.Add(adLink);
                    }
                    
                    // Minimum of 7.25 miutes throttling alone here.
                    // Then add an additional second to the request time
                    // and that is about 15 minutes to pull a link back from every add.

                    // To get around this is difficult.
                    // I'll leave that challenge for pulling back the ad content.
                    // It's going to be far larger. I still dont know the exact numbers.
                    // Lambda will have to be used to scale that work (and distribute it across IP addresses hopefully).
                    // I can divvy that wor out easily, but to do it by IP I'm still struggling with it.
                    // What would be awesome was if I knew the origin IP and can get past the server-side throttle.
                    // However, if the site isn't a piece of crap, which it doesn't seem to be,
                    // the origin servers will be white-listed to the CDN edgecast.
                    // In which case even knowing the origin IP's would be of no use whatsoever.
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }

            Console.WriteLine(usAdLinks.Count);
            foreach (var usAdLink in usAdLinks)
            {
                Console.WriteLine(usAdLink);
            }
        }

        [Test]
        public void Ends_With_Digits()
        {
            Assert.IsFalse(BackpageCrawler.LinkIsAdd("http://auburn.backpage.com/WomenSeekMen/?layout=gallery", BackpageCrawler.WOMEN_SEEKING_MEN_SECTION));
            Assert.IsFalse(BackpageCrawler.LinkIsAdd("http://auburn.backpage.com/WomenSeekMen/?layout=video", BackpageCrawler.WOMEN_SEEKING_MEN_SECTION));

            Assert.IsTrue(BackpageCrawler.LinkIsAdd("http://auburn.backpage.com/WomenSeekMen/im-back-all-3-call-me-big-bootie-red-outs-ins-205-566-56-94-ll-3-ready/24863202", BackpageCrawler.WOMEN_SEEKING_MEN_SECTION));
            Assert.IsTrue(BackpageCrawler.LinkIsAdd("http://auburn.backpage.com/WomenSeekMen/fl%CE%B1%CF%89l%D1%94%D1%95%D1%95-%D0%B2%D1%94%CE%B1%CF%85%D1%82%D1%83-%CF%83%D0%B8-%CF%85%D1%82%D1%83-%CF%81%CF%85re-%CF%81l-%CE%B1%CE%B4%CF%85r%CE%B5-/24420777", BackpageCrawler.WOMEN_SEEKING_MEN_SECTION));
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
