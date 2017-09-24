using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class ApiTests
    {
        private readonly PrivateConfig privateConfig = PrivateConfig.CreateFromPersonalJson();
        private string token;
        private const int requests = 1000;
        private const int requestDelay = 10 * 1000;

        [OneTimeSetUp]
        public void Authenticate()
        {
            // A little service point manager voodoo magic.
            // Service point manager is a factory of factories!
            // So the first request spawns the first factory,
            // locking in all values for subsequent requests.
            // Hence the term "default" and not current.
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;

            var url = $"https://tgonzalez.net/api/Gallery/token?username={privateConfig.GalleryUsername}&password={privateConfig.GalleryPassword}";
            var response = new WebClient().DownloadString(url);
            var model = JsonConvert.DeserializeObject<AuthenticationTokenModel>(response);
            token = model.Token;
        }

        /*
        [Test]
        public void Exact_Artist()
        {
            var artist = "Jean-Leon Gerome";
            var url = $"https://tgonzalez.net/api/Gallery/searchExactArtist?token={HttpUtility.UrlEncode(token)}&artist={artist}";
            var response = new WebClient().DownloadString(url);
            var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
            Assert.AreEqual(244, results.Count);
        }

        [Test]
        public void Like_Artist()
        {
            var artist = "Jean-Leon Gerome";
            var url = $"https://tgonzalez.net/api/Gallery/searchLikeArtist?token={HttpUtility.UrlEncode(token)}&artist={artist}";
            var response = new WebClient().DownloadString(url);
            var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
            Assert.AreEqual(249, results.Count);
        }
        
        [Test]
        public void Scan()
        {
            var url = $"https://tgonzalez.net/api/Gallery/scan?token={HttpUtility.UrlEncode(token)}&lastPageId=0";
            var response = new WebClient().DownloadString(url);
            var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
            Assert.AreEqual(7332, results.Count);
        }

        [Test]
        public void IP_Address()
        {
            var client = new GalleryClient();
            var ipAddress = client.GetIPAddress(token);
            Console.WriteLine("IP received by web server expected to be from CDN: " + ipAddress.IP);
            Console.WriteLine("IP recevied by web server expected to be original: " + ipAddress.OriginalVisitorIPAddress);
            Assert.AreNotEqual(privateConfig.DecryptedIp, ipAddress.OriginalVisitorIPAddress);
        }

        [Test]
        public void Wait_Time()
        {
            var client = new GalleryClient();
            var waitTime = client.GetWaitTime(token, 45 * 1000);
            Assert.AreEqual(45 * 1000, waitTime.WaitInMilliseconds);
        }
        */

        /// <summary>
        /// Something is very wrong. The default connection limit was set to int max value.
        /// The parallel loop had its throttle lifted completely.
        /// The average concurrent connections determined by measured time was 11.
        /// The max concurrent hits in the loop even was only 25 at 100 requests and going up to 250 at 1k or 10k (don't remember, but this is slow).
        /// </summary>
        [Test]
        public void Concurency()
        {
            var client = new GalleryClient();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            var sw = new Stopwatch();
            sw.Start();
            Parallel.For(1, requests, parallelOptions, connectionNumber =>
            {
                client.GetWaitTime(token, requestDelay);
            });
            sw.Stop();

            Console.WriteLine($"Performed {requests} with a delay of {requestDelay}ms taking {sw.Elapsed.TotalMinutes} minutes.");
            var projectedTimeSpan = TimeSpan.FromMilliseconds(requests * requestDelay);

            // Level of parallelism is 1
            Console.WriteLine("Estimated total request time if performed one-by-one: " + projectedTimeSpan.TotalMinutes + " minutes");

            var observedLevelOfParallelism = projectedTimeSpan.TotalMinutes / sw.Elapsed.TotalMinutes;
            Console.WriteLine($"Average level of parallelism determined by actual vs projected one-by-one is {observedLevelOfParallelism}.");
        }

        [Test]
        public void Concurency_HttpClient()
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            var sw = new Stopwatch();
            sw.Start();
            Parallel.For(1, requests, parallelOptions, connectionNumber =>
            {
                HttpClient httpClient = new HttpClient();
                var waitUrl = "https://tgonzalez.net/api/Gallery/wait" +
                              $"?token={HttpUtility.UrlEncode(token)}" +
                              $"&waitInMilliseconds={requestDelay}";
                var waitResponseString = httpClient.GetStringAsync(waitUrl).Result;
                JsonConvert.DeserializeObject<WaitTime>(waitResponseString);
            });
            sw.Stop();

            Console.WriteLine($"Performed {requests} with a delay of {requestDelay}ms taking {sw.Elapsed.TotalMinutes} minutes.");
            var projectedTimeSpan = TimeSpan.FromMilliseconds(requests * requestDelay);
            Console.WriteLine("Estimated total request time if performed one-by-one: " + projectedTimeSpan.TotalMinutes + " minutes");
            var observedLevelOfParallelism = projectedTimeSpan.TotalMinutes / sw.Elapsed.TotalMinutes;
            Console.WriteLine($"Average level of parallelism determined by actual vs projected one-by-one is {observedLevelOfParallelism}.");
        }

        [Test]
        public void Concurency_HttpClient_Reused_HttpClient()
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            HttpClient httpClient = new HttpClient();

            var sw = new Stopwatch();
            sw.Start();
            Parallel.For(1, requests, parallelOptions, connectionNumber =>
            {
                var waitUrl = "https://tgonzalez.net/api/Gallery/wait" +
                              $"?token={HttpUtility.UrlEncode(token)}" +
                              $"&waitInMilliseconds={requestDelay}";
                var waitResponseString = httpClient.GetStringAsync(waitUrl).Result;
                JsonConvert.DeserializeObject<WaitTime>(waitResponseString);

            });
            sw.Stop();

            Console.WriteLine($"Performed {requests} with a delay of {requestDelay}ms taking {sw.Elapsed.TotalMinutes} minutes.");
            var projectedTimeSpan = TimeSpan.FromMilliseconds(requests * requestDelay);
            Console.WriteLine("Estimated total request time if performed one-by-one: " + projectedTimeSpan.TotalMinutes + " minutes");
            var observedLevelOfParallelism = projectedTimeSpan.TotalMinutes / sw.Elapsed.TotalMinutes;
            Console.WriteLine($"Average level of parallelism determined by actual vs projected one-by-one is {observedLevelOfParallelism}.");
        }
        
        [Test]
        public void Concurency_HttpClient_Reused_HttpClient_WaitAll_Synchronous_Request_Firing()
        {
            HttpClient httpClient = new HttpClient();
            ConcurrentBag<Task<string>> asyncRequestResponses = new ConcurrentBag<Task<string>>();

            var sw = new Stopwatch();
            sw.Start();
            for (var connectionNumber = 0; connectionNumber < requests; connectionNumber += 1)
            {
                var waitUrl = "https://tgonzalez.net/api/Gallery/wait" +
                              $"?token={HttpUtility.UrlEncode(token)}" +
                              $"&waitInMilliseconds={requestDelay}";
                asyncRequestResponses.Add(httpClient.GetStringAsync(waitUrl));
            }

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
        public void Concurency_HttpClient_Reused_HttpClient_WaitAll_Parallel_Request_Firing()
        {
            HttpClient httpClient = new HttpClient();
            ConcurrentBag<Task<string>> asyncRequestResponses = new ConcurrentBag<Task<string>>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            var sw = new Stopwatch();
            sw.Start();
            Parallel.For(0, requests, parallelOptions, requestNumber =>
            {
                var waitUrl = "https://tgonzalez.net/api/Gallery/wait" +
                                $"?token={HttpUtility.UrlEncode(token)}" +
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
        
        /*
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
        */
    }
}
