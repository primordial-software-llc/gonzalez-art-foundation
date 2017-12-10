using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;

namespace MVC5App.Controllers
{
    /// <inheritdoc />
    /// <summary>
    /// https://docs.microsoft.com/en-us/aspnet/web-api/overview/web-api-routing-and-actions/create-a-rest-api-with-attribute-routing
    /// </summary>
    [RoutePrefix("api/Gallery")]
    public class GalleryController : ApiController
    {
        private static string authoritativeHash;
        private static readonly Authentication AUTHENTICATION = new Authentication();

        private void Authenticate(string token)
        {
            if (string.IsNullOrWhiteSpace(authoritativeHash))
            {
                var galleryUserRaw = DynamoDbClientFactory.Client.Query(
                    new QueryRequest(
                        new GalleryUser().GetTable())
                    {
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            {":id", new AttributeValue {S = "47dfa78b-9c28-41a5-9048-1df383e4c48a"}}
                        },
                        KeyConditionExpression = "id = :id",
                })
                    .Items
                    .FirstOrDefault();
                var galleryUser = Conversion<GalleryUser>.ConvertToPoco(galleryUserRaw);
                authoritativeHash = galleryUser.Hash;
            }
            if (!AUTHENTICATION.IsTokenValid(token, authoritativeHash))
            {
                throw new Exception("Not authenticated");
            }
        }

        /// <remarks>
        /// Success is determined on token usage in order to make brute force more difficult.
        /// At a minimum brute force attacks require 2x the number of requests when checking success on another service;
        /// however no additional latency is incurred for legitimate users!
        /// </remarks>
        [Route("token")]
        public AuthenticationTokenModel GetAuthenticationToken(string username, string password)
        {
            var identityHash = Authentication.GetIdentityHash(username, password);
            var response = new AuthenticationTokenModel
            {
                Token = AUTHENTICATION.GetToken(identityHash),
                ExpirationDate = AUTHENTICATION.GetUtcCalendarDayExpiration
            };
            return response;
        }

        [Route("searchLikeArtist")]
        public List<ClassificationModel> GetLike(string token, string artist)
        {
            Authenticate(token);

            var likeJeanLeonGeromeWorks = new DynamoDbClientFactory().SearchByLikeArtist(artist);
            return likeJeanLeonGeromeWorks;
        }

        [Route("searchExactArtist")]
        public List<ClassificationModel> GetExact(string token, string artist)
        {
            Authenticate(token);

            var jeanLeonGeromeWorks = new DynamoDbClientFactory().SearchByExactArtist(artist);
            return jeanLeonGeromeWorks;
        }

        [Route("scan")]
        public List<ClassificationModel> GetScanByPage(string token, int lastPageId)
        {
            Authenticate(token);

            var jeanLeonGeromeWorks = new DynamoDbClientFactory().ScanByPage(lastPageId);
            return jeanLeonGeromeWorks;
        }

        [Route("ip")]
        public RequestIPAddress GetIPAddress(string token)
        {
            Authenticate(token);

            var ipAddress = new RequestIPAddress
            {
                IP = HttpContext.Current.Request.UserHostAddress,
                OriginalVisitorIPAddress = HttpContext.Current.Request.Headers["CF-Connecting-IP"]
            };
            return ipAddress;
        }

        [Route("wait")]
        public WaitTime GetWait(string token, int waitInMilliseconds)
        {
            Authenticate(token);

            var waitTime = new WaitTime
            {
                WaitInMilliseconds = waitInMilliseconds
            };

            System.Threading.Thread.Sleep(waitTime.WaitInMilliseconds);

            return waitTime;
        }

    }
}
