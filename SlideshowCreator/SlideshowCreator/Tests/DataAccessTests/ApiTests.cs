using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using GalleryBackend;
using IndexBackend;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class ApiTests
    {
        private readonly PrivateConfig privateConfig =
            PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");
        private string token;

        [OneTimeSetUp]
        public void Authenticate()
        {
            var url = $"https://tgonzalez.net/api/Gallery/token?username={privateConfig.GalleryUsername}&password={privateConfig.GalleryPassword}";
            var response = new WebClient().DownloadString(url);
            var model = JsonConvert.DeserializeObject<AuthenticationTokenModel>(response);
            token = model.Token;
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
        
    }
}
