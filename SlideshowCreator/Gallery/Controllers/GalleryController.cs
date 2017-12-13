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
        public List<ClassificationModel> GetLike(string token, string artist, string source = null)
        {
            Authenticate(token);
            return new DynamoDbClientFactory().SearchByLikeArtist(artist, source);
        }

        [Route("searchExactArtist")]
        public List<ClassificationModel> GetExact(string token, string artist, string source = null)
        {
            Authenticate(token);
            return new DynamoDbClientFactory().SearchByExactArtist(artist, source);
        }

        [Route("searchLabel")]
        public List<ImageLabel> GetSearchByLabel(string token, string label)
        {
            Authenticate(token);
            return new DynamoDbClientFactory().SearchByLabel(label);
        }

        [Route("{pageId}/label")]
        public ImageLabel GetLabels(string token, int pageId)
        {
            Authenticate(token);
            return new DynamoDbClientFactory().GetLabel(pageId);
        }
        
        [Route("scan")]
        public List<ClassificationModel> GetScanByPage(string token, int? lastPageId, string source = null)
        {
            Authenticate(token);
            return new DynamoDbClientFactory().ScanByPage(lastPageId, source);
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

    }
}
