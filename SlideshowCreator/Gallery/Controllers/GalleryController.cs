using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.DataAccess;

namespace MVC5App.Controllers
{
    [RoutePrefix("api/Gallery")]
    public class GalleryController : ApiController
    {
        private void Authenticate()
        {
            string cookie = Request.Headers
                .GetCookies()
                .SelectMany(x => x.Cookies
                    .Where(y => y.Name.Equals("token"))
                    .Select(y => y.Value))
                .FirstOrDefault();

            Authenticate(cookie);
        }

        private void Authenticate(string token)
        {
            var client = new GalleryUserAccess(GalleryAwsCredentialsFactory.DbClient, new ConsoleLogging());
            var user = client.GetUser();
            var dbClient = new DynamoDbClient<GalleryUser>(GalleryAwsCredentialsFactory.DbClient, new ConsoleLogging());
            var auth = new Authentication(GalleryAwsCredentialsFactory.S3Client, dbClient);
            
            if (!auth.IsTokenValid(token, user.Hash))
            {
                throw new Exception("Not authenticated.");
            }
        }

        [Route("twoFactorAuthenticationRedirect")]
        public HttpResponseMessage GetTwoFactorAuthenticationRedirect(string galleryPath)
        {
            var url = HttpContext.Current.Request.Url;
            var s3Logging = new S3Logging("cloudflare-redirect-logs", GalleryAwsCredentialsFactory.S3Client);
            s3Logging.Log(url.ToString());
            var response = Request.CreateResponse(HttpStatusCode.Moved);
            var path = galleryPath.Substring(0, galleryPath.IndexOf("?"));
            var query = HttpUtility.ParseQueryString(galleryPath.Substring(galleryPath.IndexOf("?")));
            response.Headers.Location = new Uri(url.Scheme + "://" + url.Host +
                                                (url.IsDefaultPort ? "" : ":" + url.Port) +
                                                path + "?username=" + HttpUtility.UrlEncode(query.Get("username")) +
                                                        "&password=" + HttpUtility.UrlEncode(query.Get("password")));
            return response;
        }

        /// <remarks>
        /// Success is determined on token usage in order to make brute force more difficult.
        /// At a minimum brute force attacks require 2x the number of requests when checking success on another service;
        /// however no additional latency is incurred for legitimate users!
        /// </remarks>
        [Route("token")]
        public AuthenticationTokenModel GetAuthenticationToken(string username, string password)
        {
            var dbClient = new DynamoDbClient<GalleryUser>(GalleryAwsCredentialsFactory.DbClient, new ConsoleLogging());
            var auth = new Authentication(GalleryAwsCredentialsFactory.S3Client, dbClient);
            var response = new AuthenticationTokenModel
            {
                Token = auth.GetToken(Authentication.Hash($"{username}:{password}")),
                ExpirationDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")
            };
            Authenticate(response.Token);
            return response;
        }

        [Route("image/tgonzalez-image-archive/national-gallery-of-art/{s3Name}")]
        public HttpResponseMessage GetImage(string s3Name)
        {
            Authenticate();

            var key = "national-gallery-of-art/" + s3Name; // Mvc doesn't allow forward slash "/". I already "relaxed" the pathing to allowing periods.
            GetObjectResponse s3Object = GalleryAwsCredentialsFactory.S3AcceleratedClient.GetObject("tgonzalez-image-archive", key);
            var memoryStream = new MemoryStream();
            s3Object.ResponseStream.CopyTo(memoryStream);

            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new ByteArrayContent(memoryStream.ToArray());
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/" + s3Name.Split('.').Last());

            return result;
        }

        [Route("searchLikeArtist")]
        public List<ClassificationModel> GetLike(string artist, string source = null)
        {
            Authenticate();
            return new DynamoDbClientFactory().SearchByLikeArtist(artist, source);
        }

        [Route("searchExactArtist")]
        public List<ClassificationModel> GetExact(string artist, string source = null)
        {
            Authenticate();
            return new DynamoDbClientFactory().SearchByExactArtist(artist, source);
        }

        [Route("searchLabel")]
        public List<ImageLabel> GetSearchByLabel(string label, string source = null)
        {
            Authenticate();
            return new DynamoDbClientFactory().SearchByLabel(label, source);
        }

        [Route("{pageId}/label")]
        public ImageLabel GetLabels(int pageId)
        {
            Authenticate();
            return new DynamoDbClientFactory().GetLabel(pageId);
        }
        
        [Route("scan")]
        public List<ClassificationModel> GetScanByPage(int? lastPageId, string source = null)
        {
            Authenticate();
            return new DynamoDbClientFactory().ScanByPage(lastPageId, source);
        }

        [Route("ip")]
        public RequestIPAddress GetIPAddress()
        {
            Authenticate();
            var ipAddress = new RequestIPAddress
            {
                IP = HttpContext.Current.Request.UserHostAddress,
                OriginalVisitorIPAddress = HttpContext.Current.Request.Headers["CF-Connecting-IP"]
            };
            return ipAddress;
        }

    }
}
