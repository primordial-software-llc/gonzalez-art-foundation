using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using IndexBackend;
using IndexBackend.CloudFlareImUnderAttack;
using NUnit.Framework;

namespace SlideshowCreator
{
    class ImagesDotNgaDotGov
    {
        private readonly PrivateConfig privateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");

        private const string TEMP_CLEARANCE_COOKIE = "b749ef17dd8aabdf564b3c6ad900483b8a4293c4-1503447970-28800";

        [Test]
        public void Get_Home_Page_Through_500_Response()
        {
            new VpnCheck().AssertVpnInUse(privateConfig);
            var uri = new Uri(privateConfig.Target2Url);
            
            var client = new HttpClient();
            Task<HttpResponseMessage> asyncResponse = client.GetAsync(uri);
            HttpResponseMessage respone = asyncResponse.Result;

            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, respone.StatusCode);

            var html = respone.Content.ReadAsStringAsync().Result;

            // Apparently this cookie isn't required.
            //IEnumerable<Cookie> responseCookies = cookies.GetCookies(uri).Cast<Cookie>();
            // Assert.AreEqual("__cfduid", responseCookies.Single().Name);

            System.Threading.Thread.Sleep(4000);

            DecodeChallengeQuestion decodeChallengeQuestion = new DecodeChallengeQuestion();
            var clearanceUrl = decodeChallengeQuestion.GetClearanceUrl(html);

            Parallel.Invoke(
                () =>
                {
                    CookieContainer cookies = new CookieContainer();
                    HttpClientHandler handler = new HttpClientHandler { CookieContainer = cookies };
                    var authorizedClient = new HttpClient(handler);
                    Task<HttpResponseMessage> asyncClearanceResponse = authorizedClient.GetAsync(clearanceUrl);
                    HttpResponseMessage clearanceResponse = asyncClearanceResponse.Result;

                    // There's a redirect here from the clearance URL to the home page.
                    // There's actually a 302 redirect here, but HttpClient follows the redirect.
                    Assert.AreEqual(HttpStatusCode.OK, clearanceResponse.StatusCode);
                    string homePageHtml = clearanceResponse.Content.ReadAsStringAsync().Result;
                    StringAssert.Contains("/en/web_images/boatmen3.jpg", homePageHtml);

                    // This is what grants access. This is what I worked all night to generate.
                    var clearanceCookie = cookies.GetCookies(uri).Cast<Cookie>().Single(x => x.Name.Equals("cf_clearance"));
                    Console.WriteLine(clearanceCookie);

                    // This is the proper home page of the website. From here we can download the super high resolution images which are so BIG, they are zipped. I'm drooling at howa an HD Jean-Leon Gerome will look on my HD projector!
                    Assert.AreEqual("http://images.nga.gov/en/page/show_home_page.html", clearanceResponse.RequestMessage.RequestUri.AbsoluteUri);
                },
                () =>
                {
                    
                    System.Threading.Thread.Sleep(100);
                    CookieContainer cookies = new CookieContainer();
                    HttpClientHandler handler = new HttpClientHandler { CookieContainer = cookies };
                    var authorizedClient = new HttpClient(handler);
                    Task<HttpResponseMessage> asyncClearanceResponse = authorizedClient.GetAsync(clearanceUrl);
                    HttpResponseMessage clearanceResponse = asyncClearanceResponse.Result;

                    // There's a redirect here from the clearance URL to the home page.
                    // There's actually a 302 redirect here, but HttpClient follows the redirect.
                    Assert.AreEqual(HttpStatusCode.OK, clearanceResponse.StatusCode);
                    string homePageHtml = clearanceResponse.Content.ReadAsStringAsync().Result;
                    StringAssert.Contains("/en/web_images/boatmen3.jpg", homePageHtml);

                    // This is what grants access. This is what I worked all night to generate.
                    var clearanceCookie = cookies.GetCookies(uri).Cast<Cookie>().Single(x => x.Name.Equals("cf_clearance"));
                    Console.WriteLine(clearanceCookie);

                    // This is the proper home page of the website. From here we can download the super high resolution images which are so BIG, they are zipped. I'm drooling at howa an HD Jean-Leon Gerome will look on my HD projector!
                    Assert.AreEqual("http://images.nga.gov/en/page/show_home_page.html", clearanceResponse.RequestMessage.RequestUri.AbsoluteUri);
                    
                },
                () =>
                {

                    System.Threading.Thread.Sleep(100);
                    CookieContainer cookies = new CookieContainer();
                    HttpClientHandler handler = new HttpClientHandler { CookieContainer = cookies };
                    var authorizedClient = new HttpClient(handler);
                    Task<HttpResponseMessage> asyncClearanceResponse = authorizedClient.GetAsync(clearanceUrl);
                    HttpResponseMessage clearanceResponse = asyncClearanceResponse.Result;

                    // There's a redirect here from the clearance URL to the home page.
                    // There's actually a 302 redirect here, but HttpClient follows the redirect.
                    Assert.AreEqual(HttpStatusCode.OK, clearanceResponse.StatusCode);
                    string homePageHtml = clearanceResponse.Content.ReadAsStringAsync().Result;
                    StringAssert.Contains("/en/web_images/boatmen3.jpg", homePageHtml);

                    // This is what grants access. This is what I worked all night to generate.
                    var clearanceCookie = cookies.GetCookies(uri).Cast<Cookie>().Single(x => x.Name.Equals("cf_clearance"));
                    Console.WriteLine(clearanceCookie);

                    // This is the proper home page of the website. From here we can download the super high resolution images which are so BIG, they are zipped. I'm drooling at howa an HD Jean-Leon Gerome will look on my HD projector!
                    Assert.AreEqual("http://images.nga.gov/en/page/show_home_page.html", clearanceResponse.RequestMessage.RequestUri.AbsoluteUri);

                });




        }

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

            var html = readStream.ReadToEnd();
            Console.WriteLine(html);

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
