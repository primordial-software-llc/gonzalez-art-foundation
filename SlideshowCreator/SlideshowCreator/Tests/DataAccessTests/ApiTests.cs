using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using GalleryBackend;
using IndexBackend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class ApiTests
    {
        private readonly PrivateConfig privateConfig = PrivateConfig.CreateFromPersonalJson();
        private string token;

        [OneTimeSetUp]
        public void Authenticate()
        {

        }

        [Test]
        public void B_Exact_Artist()
        {
            var artist = "Jean-Leon Gerome";
            var url = $"https://tgonzalez.net/api/Gallery/searchExactArtist?token={HttpUtility.UrlEncode(token)}&artist={artist}";
            var response = new WebClient().DownloadString(url);
            var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
            Assert.AreEqual(244, results.Count);
        }

        [Test]
        public void C_Like_Artist()
        {
            var artist = "Jean-Leon Gerome";
            var url = $"https://tgonzalez.net/api/Gallery/searchLikeArtist?token={HttpUtility.UrlEncode(token)}&artist={artist}";
            var response = new WebClient().DownloadString(url);
            var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
            Assert.AreEqual(249, results.Count);
        }
        
        [Test]
        public void D_Scan()
        {
            var url = $"https://tgonzalez.net/api/Gallery/scan?token={HttpUtility.UrlEncode(token)}&lastPageId=0";
            var response = new WebClient().DownloadString(url);
            var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
            Assert.AreEqual(7332, results.Count);
        }

        [Test]
        public void D_IP_Address_Test()
        {
            var client = new GalleryClient();
            var ipAddress = client.GetIPAddress(token);
            Console.WriteLine("IP received by web server expected to be from CDN: " + ipAddress.IP);
            Console.WriteLine("IP recevied by web server expected to be original: " + ipAddress.OriginalVisitorIPAddress);
            Assert.AreNotEqual(privateConfig.DecryptedIp, ipAddress.OriginalVisitorIPAddress);
        }

        [Test]
        public void Forced_Wait_Time()
        {
            var client = new GalleryClient();
            var waitTime = client.GetWaitTime(token, 45 * 1000);
            Assert.AreEqual(45 * 1000, waitTime.WaitInMilliseconds);
        }

        [Test]
        public void Test_Conn_Limit()
        {
            Console.WriteLine(ServicePointManager.DefaultConnectionLimit);
        }

        [Test]
        public void Concurency()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue; // First requests locks in the defualt on the service point.
            // The default is being set on a factories constructor
            // Not within the constructor contained by the factory.
            var url = $"https://tgonzalez.net/api/Gallery/token?username={privateConfig.GalleryUsername}&password={privateConfig.GalleryPassword}";
            var response = new WebClient().DownloadString(url);
            var model = JsonConvert.DeserializeObject<AuthenticationTokenModel>(response);
            token = model.Token;

            var client = new GalleryClient();

            ConcurrentBag<int> connectionCount = new ConcurrentBag<int>();
            ConcurrentBag<int> levelOfParallelism = new ConcurrentBag<int>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            Parallel.For(1, 10000, parallelOptions, connectionNumber =>
            {
                connectionCount.Add(connectionNumber);
                try
                {
                    client.GetWaitTime(token, 5 * 1000);
                    levelOfParallelism.Add(connectionCount.Count);
                }
                finally
                {
                    connectionCount.TryTake(out int removedItem);
                }
            });
            Assert.AreEqual(0, connectionCount.Count);
            int min = levelOfParallelism.Min(x => x);
            Console.WriteLine("Min parallelism: " + min);
            Console.WriteLine("Max parallelism achieved: " + levelOfParallelism.Max(x => x));

            // We're only getting 12 parallel connections with a count of 1,000
            // and 239 parallel connections with a count of 10,000.
            // Memory and CPU aren't evn moving in any noticable amount.
            // That's not good.
            // The parallel for each is underperforming for the hardware.
            // Once we start to max out the hardware we can know we are saturating the connection.
            // Once we saturate the connection, we know the saturation point and can tone down
            // the level of parallelism.
            // None of that can be determined if the parallel foreach loop isn't going high in concurrency.
            // Keep in mind that this isn't even actual open connections.
            // The connection could still be throttled between Add and GetWaitTime.
            // So being only 12 is too low.

            // Need http client and finer control of threads.
            // https://stackoverflow.com/questions/24904719/how-to-limit-connections-with-async-httpclient-c-sharp
            // Test is setup well though.
        }

        [Test]
        public void Test()
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
