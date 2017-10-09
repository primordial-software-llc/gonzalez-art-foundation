using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using IndexBackend.Backpage;
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

        public List<BackpageAd> GetAdLinksFromSection(Uri uri, string section, int attempt = 0)
        {
            string sampleLinkWomenSeekingMen = uri + section;
            List<BackpageAd> adUris;
            HttpResponseMessage response = null;
            string html = null;

            try
            {
                response = Client.GetAsync(sampleLinkWomenSeekingMen).Result;
                html = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();
                adUris = ParseAdLinks(html, section);
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
                    var waitPeriod = TimeSpan.FromSeconds(60);
                    Console.WriteLine($"Waiting {waitPeriod.TotalSeconds} seconds for " + uri);
                    System.Threading.Thread.Sleep(waitPeriod);
                    Console.WriteLine("Retrying " + uri);
                    adUris = GetAdLinksFromSection(uri, section, attempt + 1);
                }
                else
                {
                    throw;
                }
            }

            // If the ad appears multiple times, give me the ad with an age over the duplicate that doesn't have an age.
            // This occurs on the sponsored ads on the side. I can't say with 100% certainty that they aren't all duplicates.
            adUris = adUris.OrderByDescending(x => x.Age).ToList();
            adUris = adUris.GroupBy(x => x.Uri).Select(x => x.First()).ToList();

            return adUris;
        }

        public List<BackpageAd> ParseAdLinks(string html, string section)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var links = new List<BackpageAd>();

            var linkElements = htmlDoc.DocumentNode.Descendants("a");
            foreach (var linkElement in linkElements)
            {
                var link = linkElement.Attributes["href"].Value;
                var text = linkElement.InnerText;
                if (LinkIsAdd(link, section) &&
                    !string.IsNullOrWhiteSpace(text)) // Could be an image link, in which case the text link should exist as well. The gallery portion isn't being used the text ads are.
                {
                    var ad = new BackpageAd {Uri = new Uri(link)};
                    var textWords = text.Split(' ');
                    var age = textWords.Last();

                    if (IsNumber(age) &&
                        textWords.Length >= 2 &&
                        textWords[textWords.Length - 2].Equals("-")) // Check for age delimiter to eliminate side-ads without age.
                    {
                        ad.Age = int.Parse(age);
                    }

                    links.Add(ad);
                }
            }

            return links;
        }

        public static bool LinkIsAdd(string link, string section)
        {
            if (!link.ToLower().Contains(section.ToLower()))
            {
                return false;
            }

            return Regex.IsMatch(link, "\\d+(\\.\\d+)?$");
        }

        public static bool IsNumber(string value)
        {
            return Regex.IsMatch(value, @"^\d+$");
        }

    }
}
