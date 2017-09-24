using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class ApiTests
    {
        private readonly PrivateConfig privateConfig = PrivateConfig.CreateFromPersonalJson();
        private GalleryClient client;

        [OneTimeSetUp]
        public void Authenticate()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            client = new GalleryClient(privateConfig.GalleryUsername, privateConfig.GalleryPassword);
        }
        
        [Test]
        public void Exact_Artist()
        {
            var artist = "Jean-Leon Gerome";
            var results = client.SearchExactArtist(artist);
            Assert.AreEqual(244, results.Count);
        }

        [Test]
        public void Like_Artist()
        {
            var artist = "Jean-Leon Gerome";
            var results = client.SearchLikeArtist(artist);
            Assert.AreEqual(249, results.Count);
        }
        
        [Test]
        public void Scan()
        {
            var results = client.Scan();
            Assert.AreEqual(7332, results.Count);
        }

        [Test]
        public void IP_Address()
        {
            var ipAddress = client.GetIPAddress();
            Console.WriteLine("IP received by web server expected to be from CDN: " + ipAddress.IP);
            Console.WriteLine("IP recevied by web server expected to be original: " + ipAddress.OriginalVisitorIPAddress);
            Assert.AreNotEqual(privateConfig.DecryptedIp, ipAddress.OriginalVisitorIPAddress);
        }

        [Test]
        public void Wait_Time()
        {
            var waitMs = 100;
            var waitTime = client.GetWaitTime(waitMs);
            Assert.AreEqual(waitMs, waitTime.WaitInMilliseconds);
        }
        
        [Test]
        public void Concurency_HttpClient_Reused_HttpClient_WaitAll_Parallel_Request_Firing()
        {
            HttpClient httpClient = new HttpClient();
            ConcurrentBag<Task<string>> asyncRequestResponses = new ConcurrentBag<Task<string>>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            const int requests = 100;
            const int requestDelay = 1 * 1000;

            var sw = new Stopwatch();
            sw.Start();
            Parallel.For(0, requests, parallelOptions, requestNumber =>
            {
                var waitUrl = "https://tgonzalez.net/api/Gallery/wait" +
                                $"?token={HttpUtility.UrlEncode(client.Token)}" +
                                $"&waitInMilliseconds={requestDelay}";
                asyncRequestResponses.Add(httpClient.GetStringAsync(waitUrl));
            });

            Task.WhenAll(asyncRequestResponses);

            foreach (var asyncRequestResponse in asyncRequestResponses)
            {
                JsonConvert.DeserializeObject<WaitTime>(asyncRequestResponse.Result);
            }
            sw.Stop();

            Console.WriteLine($"Performed {requests} with a delay of {requestDelay}ms taking {sw.Elapsed.TotalMinutes} minutes.");
            var projectedTimeSpan = TimeSpan.FromMilliseconds(requests * requestDelay);
            Console.WriteLine("Estimated total request time if performed one-by-one: " + projectedTimeSpan.TotalMinutes + " minutes");
            var observedLevelOfParallelism = projectedTimeSpan.TotalMinutes / sw.Elapsed.TotalMinutes;
            Console.WriteLine($"Average level of parallelism determined by actual vs projected one-by-one is {observedLevelOfParallelism}.");
        }

        [Test]
        public void Check_For_Secrets_In_Source_Code()
        {
            var files = Directory.EnumerateFiles(
                "C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator",
                "*.*",
                SearchOption.AllDirectories
            ).ToList();
            files.Remove(PrivateConfig.PersonalJson);

            var gitIgnore = File.ReadAllLines("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\.gitignore");

            Assert.IsTrue(gitIgnore.Contains(PrivateConfig.PersonalJson.Split('\\').Last()));

            var secrets = File.ReadAllText(PrivateConfig.PersonalJson);
            var secretsJson = JObject.Parse(secrets);
            
            int filesChecked = 0;
            foreach (string file in files)
            {
                try
                {
                    var data = File.ReadAllText(file, Encoding.UTF8);
                    foreach (var secretJson in secretsJson)
                    {
                        var secretValue = secretJson.Value.ToString().ToLower();
                        if (data.ToLower().Contains(secretValue))
                        {
                            throw new Exception($"Secret {secretValue} discovered in source code at: " + file);
                        }
                    }
                    filesChecked += 1;
                }
                catch (IOException e)
                {
                    Console.WriteLine("Skipped: " + e.Message);
                }
            }
            Console.WriteLine($"checked {filesChecked} of {files.Count} files.");
        }

    }
}
