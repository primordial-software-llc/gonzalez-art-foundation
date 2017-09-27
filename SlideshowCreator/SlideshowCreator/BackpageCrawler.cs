using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using NUnit.Framework;

namespace SlideshowCreator
{
    class BackpageCrawler
    {
        public static Uri UnitedStatesHomePage => new Uri("http://us.backpage.com/");
        public const string WOMEN_SEEKING_MEN_SECTION = "WomenSeekMen/";

        private HttpClient client;
        public HttpClient Client
        {
            get
            {
                if (client == null)
                {
                    HttpClientHandler httpClientHandler = new HttpClientHandler { AllowAutoRedirect = false };
                    client = new HttpClient(httpClientHandler);
                    client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                }
                return client;
            }
        }

        public Dictionary<string, List<Uri>> GetGeographicLocationLinks(Uri url)
        {
            Dictionary<string, List<Uri>> geographicLocationLinkDictionary = new Dictionary<string, List<Uri>>();

            var response = Client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            var html = response.Content.ReadAsStringAsync().Result;
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var firstLevelGeographicLocations = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'geoUnit')]");

            Assert.AreEqual(51, firstLevelGeographicLocations.Count); // Washington DC isn't a state. It astounds me how organized this site is.
            foreach (var firstLevelGeographicLocation in firstLevelGeographicLocations)
            {
                var geographicLocationHeader = firstLevelGeographicLocation.Descendants("h3").Single();
                var geographicLocationName = geographicLocationHeader.InnerText;

                List<Uri> geographicLocationLinks = new List<Uri>();
                geographicLocationLinkDictionary.Add(geographicLocationName, geographicLocationLinks);

                geographicLocationLinks.AddRange(
                    firstLevelGeographicLocation
                        .Descendants("ul")
                        .Single()
                        .Descendants("li")
                        .Select(x => new Uri(
                            x.Descendants("a")
                                .Single()
                                .Attributes["href"]
                                .Value)).ToList()
                );

                if (!geographicLocationLinks.Any())
                {
                    string smallGeographicLocationLink = geographicLocationHeader
                        .Descendants("a").Single()
                        .Attributes["href"].Value;
                    geographicLocationLinks.Add(new Uri(smallGeographicLocationLink));
                }
            }

            return geographicLocationLinkDictionary;
        }

        public List<Uri> GetAdLinksFromSection(Uri uri, string section, int attempt = 0)
        {
            string sampleLinkWomenSeekingMen = uri + section;
            List<Uri> adUris;
            HttpResponseMessage response = null;
            string html = null;

            try
            {
                response = Client.GetAsync(sampleLinkWomenSeekingMen).Result;
                html = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();
                adUris = ParseAdLinks(html);
            }
            catch (Exception exception) // Network error where a response may not exist if the connection gets closed. Not a timeout where the connection just stays open.
            {
                AggregateException aggregateException = (exception as AggregateException)?.Flatten();
                string message = "Failed on " + uri;

                if (aggregateException != null)
                {
                    var exceptionMessages = aggregateException.InnerExceptions.Select(x => x.Message).ToList();
                    message += " Exception: " + string.Join(", ", exceptionMessages);
                }
                else
                {
                    message += " Exception: " + exception.Message;
                }

                if (response != null)
                {
                    message += " Status Code: " + (int)response.StatusCode;
                }
                if (html != null)
                {
                    message += " Response Body: " + html;
                }

                Console.WriteLine(message);

                if (attempt == 0 &&
                    (aggregateException != null &&
                        aggregateException
                            .InnerExceptions
                            .Any(x => x is HttpRequestException) ||
                    exception is HttpRequestException))
                {
                    Console.WriteLine("Waiting 30 seonds for " + uri);
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(30));
                    Console.WriteLine("Retrying " + uri);
                    adUris = GetAdLinksFromSection(uri, section, attempt + 1);
                }
                else
                {
                    throw;
                }
            }

            return adUris;
        }

        public List<Uri> ParseAdLinks(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var ads = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'cat')]");
            var adLinks = ads.Select(x => x.Descendants("a").Single().Attributes["href"].Value);
            return adLinks.Select(x => new Uri(x)).ToList();
        }

    }
}
