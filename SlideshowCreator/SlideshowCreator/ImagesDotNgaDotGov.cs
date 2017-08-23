using System;
using System.IO;
using System.Net;
using System.Text;
using NUnit.Framework;

namespace SlideshowCreator
{
    class ImagesDotNgaDotGov
    {
        private readonly PrivateConfig privateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");

        private const string TEMP_CLEARANCE_COOKIE = "b749ef17dd8aabdf564b3c6ad900483b8a4293c4-1503447970-28800";

        [Test]
        public void Get_Home_Page()
        {
            new VpnCheck().AssertVpnInUse(privateConfig);
            var req = (HttpWebRequest) WebRequest.Create(privateConfig.Target2Url); // I'm hiding the url's as a safety, because what happens if I direct a crawler to the site and they abuse the site's intended rate limit?
            req.Headers.Add("Cache-Control", "max-age=0");
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.101 Safari/537.36";
            req.Headers.Add("Accept-Encoding", "gzip, deflate");
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            req.Headers.Add("Cookie", $"cf_clearance={TEMP_CLEARANCE_COOKIE};");
            req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            var response = req.GetResponse();

            var responseStream = response.GetResponseStream();
            StreamReader readStream = new StreamReader(responseStream, Encoding.UTF8);

            var text = readStream.ReadToEnd();
            Console.WriteLine(text);

            response.Close();
            responseStream.Close();
        }

        [Test]
        public void Decode_High_Res_Image_Reference()
        {
            var dataParam = "JTdCJTIybWFpbkZvcm0lMjIlM0ElN0IlMjJwcm9qZWN0X3RpdGxlJTIyJTNBJTIyUGVyc29uYWwlMjBEaWdpdGFsJTIwR2FsbGVyeSUyMiUyQyUyMnVzYWdlJTIyJTNBJTIyNSUyMiU3RCUyQyUyMmFzc2V0cyUyMiUzQSU3QiUyMmEwJTIyJTNBJTdCJTIyYXNzZXRJZCUyMiUzQSUyMjEzNTc0OSUyMiUyQyUyMnNpemVJZCUyMiUzQSUyMjMlMjIlN0QlN0QlN0Q=";
            byte[] base64 = Convert.FromBase64String(dataParam);
            string decodedBase64 = Encoding.UTF8.GetString(base64);
            Console.WriteLine(decodedBase64);

            var urlDecoded = WebUtility.UrlDecode(decodedBase64);
            Console.WriteLine(urlDecoded);
            Assert.AreEqual(@"{""mainForm"":{""project_title"":""Personal Digital Gallery"",""usage"":""5""},""assets"":{""a0"":{""assetId"":""135749"",""sizeId"":""3""}}}", urlDecoded);
        }
    }
}
