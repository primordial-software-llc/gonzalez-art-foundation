using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.DataAccess;
using NUnit.Framework;
using SlideshowCreator.Backpage;

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

            var url = cofig.SomeUrl + Guid.NewGuid(); // Not really a secret anymore. The site is public, there's nothing to hide.
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
        public void Test_Ad_Links_From_Second_Level_Link()
        {
            var adLinksInGeographicRegion = backpageCrawler.GetAdLinksFromSection(
                new Uri("http://losangeles.backpage.com"),
                BackpageCrawler.WOMEN_SEEKING_MEN_SECTION);

            Console.WriteLine(adLinksInGeographicRegion.Count);

            Assert.IsTrue(adLinksInGeographicRegion.Count > 200);

            foreach (var link in adLinksInGeographicRegion)
            {
                Console.WriteLine(link.Uri);
            }

            Assert.IsFalse(adLinksInGeographicRegion
                .Any(x => x.Uri.AbsoluteUri.ToLower().StartsWith("http://losangeles.backpage.com/WomenSeekMen/?page=", StringComparison.OrdinalIgnoreCase)));
        }

        [Test]
        public void Print_Us_Link_Dictionary()
        {
            foreach (KeyValuePair<string, List<Uri>> links in linkDictionary)
            {
                foreach (var geographicLink in links.Value)
                {
                    Console.WriteLine(geographicLink.AbsoluteUri);
                }
            }
        }

        [Test]
        public void Index_All_AdLinksInUsOnFirstPageOfEachSection()
        {
            Assert.AreEqual(51, linkDictionary.Count);
            Assert.IsTrue(linkDictionary.All(x => x.Value.Count > 0));
            Assert.AreEqual(435, linkDictionary.Values.Sum(x => x.Count));

            List<BackpageAdModel> usAds = new List<BackpageAdModel>();

            foreach (KeyValuePair<string, List<Uri>> links in linkDictionary)
            {
                foreach (var geographicLink in links.Value)
                {
                    var adLinksInGeographicRegion = backpageCrawler.GetAdLinksFromSection(geographicLink, BackpageCrawler.WOMEN_SEEKING_MEN_SECTION);

                    foreach (var adLink in adLinksInGeographicRegion)
                    {
                        usAds.Add(adLink);
                    }
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }

            var adsThirtyAndUnder = usAds.Where(x => x.Age <= 30 || x.Age == 0).ToList();
            Console.WriteLine("Indexing ads for persons aged thirty and under:");
            Console.WriteLine(adsThirtyAndUnder.Count);

            var access = new BackpageAdAccess();
            var client = new AwsClientFactory().CreateDynamoDbClient();
            foreach (var usAd in adsThirtyAndUnder)
            {
                access.Insert(client, usAd);
                Console.WriteLine("Indexed - " + usAd.Age + ": " + usAd.Uri);
            }
        }

        [Test]
        public void Is_Number_Tests()
        {
            Assert.IsFalse(BackpageCrawler.IsNumber("🌟🌟🌟"));
            Assert.IsFalse(BackpageCrawler.IsNumber("3🚪💯"));
            Assert.IsFalse(BackpageCrawler.IsNumber("-3"));
            Assert.IsFalse(BackpageCrawler.IsNumber("a4"));
            Assert.IsFalse(BackpageCrawler.IsNumber("4b"));

            Assert.IsTrue(BackpageCrawler.IsNumber("4"));
            Assert.IsTrue(BackpageCrawler.IsNumber("24"));
            Assert.IsTrue(BackpageCrawler.IsNumber("29"));
            Assert.IsTrue(BackpageCrawler.IsNumber("37"));
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
