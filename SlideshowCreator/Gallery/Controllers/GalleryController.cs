using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Http;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using Newtonsoft.Json;

namespace MVC5App.Controllers
{
    [RoutePrefix("api/Gallery")]
    public class GalleryController : ApiController
    {
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

        [Route("token")]
        public HttpResponseMessage  GetAuthenticationToken(string username, string password)
        {
            var dbClient = GalleryAwsCredentialsFactory.DbClient;
            var awsToolsClient = new DynamoDbClient<GalleryUser>(dbClient, new ConsoleLogging());
            var userClient = new GalleryUserAccess(dbClient, new ConsoleLogging(),
                awsToolsClient,
                new S3Logging("token-salt-cycling", GalleryAwsCredentialsFactory.S3AcceleratedClient));
            var auth = new Authentication(userClient);
            var token = auth.GetToken(Authentication.Hash($"{username}:{password}"));
            var masterHash = awsToolsClient.Get(new GalleryUser {Id = Authentication.MASTER_USER_ID}).Result.Hash;

            if (!auth.IsTokenValid(token, masterHash))
            {
                var accessIssues= "Headers" + Environment.NewLine + Request.Headers;
                var s3Logging = new S3Logging("denied-ip-access-logs", GalleryAwsCredentialsFactory.S3Client);
                s3Logging.Log(accessIssues);
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            var response = new AuthenticationTokenModel();
            response.Token = token;
            response.ExpirationDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponse.Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json");

            return httpResponse;
        }

        [Route("image/tgonzalez-image-archive/national-gallery-of-art/{s3Name}")]
        public HttpResponseMessage GetImage(string s3Name)
        {
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
            return new DynamoDbClientFactory().SearchByLikeArtist(artist, source);
        }

        [Route("searchExactArtist")]
        public List<ClassificationModel> GetExact(string artist, string source = null)
        {
            return new DynamoDbClientFactory().SearchByExactArtist(artist, source);
        }

        [Route("searchLabel")]
        public List<ImageLabel> GetSearchByLabel(string label, string source = null)
        {
            return new DynamoDbClientFactory().SearchByLabel(label, source);
        }

        [Route("{pageId}/label")]
        public ImageLabel GetLabels(int pageId)
        {
            return new DynamoDbClientFactory().GetLabel(pageId);
        }
        
        [Route("scan")]
        public List<ClassificationModel> GetScanByPage(int? lastPageId, string source = null)
        {
            return new DynamoDbClientFactory().ScanByPage(lastPageId, source);
        }

        [Route("ip")]
        public RequestIPAddress GetIPAddress()
        {
            var ipAddress = new RequestIPAddress
            {
                IP = HttpContext.Current.Request.UserHostAddress,
                OriginalVisitorIPAddress = HttpContext.Current.Request.Headers["CF-Connecting-IP"]
            };
            return ipAddress;
        }

    }
}
