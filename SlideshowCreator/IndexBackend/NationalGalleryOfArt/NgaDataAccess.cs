using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IndexBackend.CloudFlareImUnderAttack;

namespace IndexBackend.NationalGalleryOfArt
{
    public class NgaDataAccess
    {
        protected virtual HttpClient Client { get; set; }
/// <summary>
        /// Process:
        /// 1. Hit images.nga.gov
        /// 2. Receive a 5xxx response, however there is an HTML page with a challene form.
        /// The challenge form has the answer obfuscated and encoded in a JavaScript array.
        /// 3. Parse the obfuscated JavaScript then decode the challenge question.
        /// 3. Wait 4 seconds for the DDOS protection to allow the challenge form to succeed (even with the correct answer).
        /// 4. Get a cf_clearance cookie from the challenge form response authorizing acess to the home page
        /// 5. Get a 3xxx redirect from the challenge form with the cookie back to the home page.
        /// 
        /// Note: I don't know how long the cf_clearance cookie is valid for.
        /// 
        /// If you don't wait 4 seconds or pass the correct answer to get a cf_clearance token, you will continually be redirected to the challenge form until it is correctly filled out.
        /// 
        /// In case if this process starts to fail, this is what you want back for access: cookies.GetCookies(baseUri).Cast<Cookie>().Single(x => x.Name.Equals("cf_clearance"));
        /// 
        /// It should look like: cf_clearance=9f3c85ef0ab6bce1319ff978756dc1e0992e1be3-1505600061-28800
        /// </summary>
        /// <remarks>
        /// I've chosen to use strict decoding of the pattern and a plain XML document loader
        /// out of fear for turning the crawler against itself. I don't want to execute javascript that I can't review
        /// nor do I want to automatically point a web browser to a page that I don't know exactly what is there.
        /// Doing this type of work will come in time, but not yet.
        /// That would have to be run on a dedicated server.
        /// It would be very easy to inject something to turn the process against itself or onto another target.
        /// However, it would be difficult to attack string parsing with arithmetic or XML document parsing.
        /// So when I start crawling, I need servers that I can assume to be infected with malware.
        /// Not development machines which have some sensitive data (I try and limit what persists on my development machine and use dedicated storage computers for sensitive information - eventually input only with output only being a monitor with transiently decrypted data, but I'm not there yet).
        /// 
        /// It may be a good idea to get there before getting into crawling.
        /// </remarks>
        public void Init(Uri baseUri)
        {
            CookieContainer cookies = new CookieContainer();
            HttpClientHandler cookieHandler = new HttpClientHandler { CookieContainer = cookies };
            Client = new HttpClient(cookieHandler);

            Task<HttpResponseMessage> asyncResponse = Client.GetAsync(baseUri);
            HttpResponseMessage respone = asyncResponse.Result;

            var html = respone.Content.ReadAsStringAsync().Result;

            System.Threading.Thread.Sleep(4000);

            DecodeChallengeQuestion decodeChallengeQuestion = new DecodeChallengeQuestion();
            var clearanceUrl = decodeChallengeQuestion.GetClearanceUrl(html);

            Task<HttpResponseMessage> asyncClearanceResponse = Client.GetAsync(clearanceUrl);
            HttpResponseMessage clearanceResponse = asyncClearanceResponse.Result;

            clearanceResponse.EnsureSuccessStatusCode();
        }

        public void DownloadHighResImageZipFileIfExists(int assetId, string path)
        {
            var zipFile = GetHighResImageZipFile(assetId);

            if (zipFile != null)
            {
                File.WriteAllBytes(path + "\\image-bundle-" + assetId + ".zip", zipFile);
            }
        }

        public byte[] GetHighResImageZipFile(int assetId)
        {
            var encodedReference = HighResImageEncoding.CreateReferenceUrlData(assetId);
            var imageDownloadUrl =
                $"http://images.nga.gov/?service=basket&action=do_direct_download&type=dam&data={encodedReference}";
            Console.WriteLine(imageDownloadUrl);
            Task<byte[]> asyncImageResponse = Client.GetByteArrayAsync(imageDownloadUrl);
            byte[] imageZipFile = asyncImageResponse.Result;
            
            string imageResponseText = Encoding.UTF8.GetString(imageZipFile);

            var zipPrefix = "PK\u0003\u0004";
            if (!imageResponseText.StartsWith(zipPrefix))
            {
                imageZipFile = null;
            }

            return imageZipFile;
        }
        
    }
}
