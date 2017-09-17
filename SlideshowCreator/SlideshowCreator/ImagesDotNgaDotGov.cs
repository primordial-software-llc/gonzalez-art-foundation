using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using IndexBackend;
using IndexBackend.CloudFlareImUnderAttack;
using IndexBackend.NationalGalleryOfArt;
using NUnit.Framework;

namespace SlideshowCreator
{
    class ImagesDotNgaDotGov
    {
        private readonly PrivateConfig privateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");
        
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

            var assetId = 46482;
            var encodedReference = HighResImageEncoding.CreateReferenceUrlData(assetId);
            var imageDownloadUrl =
                $"http://images.nga.gov/?service=basket&action=do_direct_download&type=dam&data={encodedReference}";
            Console.WriteLine(imageDownloadUrl);
            Task<byte[]> asyncImageResponse = authorizedClient.GetByteArrayAsync(imageDownloadUrl);
            byte[] imageResponse = asyncImageResponse.Result;
            File.WriteAllBytes("C:\\Data\\NationalGalleryOfArtZippedImages\\image-bundle-" + assetId + ".zip", imageResponse);
        }

        private const string DECODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE =
                @"{""mainForm"":{""project_title"":""Personal Digital Gallery"",""usage"":""5""},""assets"":{""a0"":{""assetId"":""135749"",""sizeId"":""3""}}}";

        public const string ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE = "JTdCJTIybWFpbkZvcm0lMjIlM0ElN0IlMjJwcm9qZWN0X3RpdGxlJTIyJTNBJTIyUGVyc29uYWwlMjBEaWdpdGFsJTIwR2FsbGVyeSUyMiUyQyUyMnVzYWdlJTIyJTNBJTIyNSUyMiU3RCUyQyUyMmFzc2V0cyUyMiUzQSU3QiUyMmEwJTIyJTNBJTdCJTIyYXNzZXRJZCUyMiUzQSUyMjEzNTc0OSUyMiUyQyUyMnNpemVJZCUyMiUzQSUyMjMlMjIlN0QlN0QlN0Q=";

        public const string URL_ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE = "%7B%22mainForm%22%3A%7B%22project_title%22%3A%22Personal%20Digital%20Gallery%22%2C%22usage%22%3A%225%22%7D%2C%22assets%22%3A%7B%22a0%22%3A%7B%22assetId%22%3A%22135749%22%2C%22sizeId%22%3A%223%22%7D%7D%7D";

        [Test]
        public void Decode_High_Res_Image_Reference()
        {
            Assert.AreEqual(DECODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE, 
                HighResImageEncoding.Decode(ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE));
        }

        [Test]
        public void Generate_Encoded_High_Res_Image_Reference()
        {
            Assert.AreEqual(ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE,
                HighResImageEncoding.Encode(DECODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE));
        }

        [Test]
        public void Generate_Encoded_High_Res_Image_Reference_From_Typed_Model()
        {
            var reference = HighResImageEncoding.CreateReferenceUrlData(135749);
            Assert.AreEqual(ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE,
                reference);
        }

    }
}
