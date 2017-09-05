﻿using System.Collections.Generic;
using System.Net;
using System.Web;
using GalleryBackend;
using GalleryBackend.Classification;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.DataAccessTests
{
    class ApiTests
    {
        private string token;

        [Test]
        public void A_Authenticate()
        {
            var privateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");

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
            Assert.AreEqual(233, results.Count);
        }

        [Test]
        public void C_Like_Artist()
        {
            var artist = "Jean-Leon Gerome";
            var url = $"https://tgonzalez.net/api/Gallery/searchLikeArtist?token={HttpUtility.UrlEncode(token)}&artist={artist}";
            var response = new WebClient().DownloadString(url);
            var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
            Assert.AreEqual(237, results.Count);
        }
        
        [Test]
        public void D_Scan()
        {
            var url = $"https://tgonzalez.net/api/Gallery/scan?token={HttpUtility.UrlEncode(token)}&lastPageId=0";
            var response = new WebClient().DownloadString(url);
            var results = JsonConvert.DeserializeObject<List<ClassificationModel>>(response);
            Assert.AreEqual(7350, results.Count);
        }
        
    }
}
