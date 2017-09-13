﻿using System;
using System.Collections.Generic;
using System.Web.Http;
using GalleryBackend;
using IndexBackend;

namespace MVC5App.Controllers
{
    /// <inheritdoc />
    /// <summary>
    /// https://docs.microsoft.com/en-us/aspnet/web-api/overview/web-api-routing-and-actions/create-a-rest-api-with-attribute-routing
    /// </summary>
    [RoutePrefix("api/Gallery")]
    public class GalleryController : ApiController
    {
        const string DISCLOSED_IDENTITY_HASH = "3vwD/tk27FM5baxW1aEh+C6DGjS7Jr5FH9/RtsuH4Lk=";
        private static readonly Authentication Authentication = new Authentication(DISCLOSED_IDENTITY_HASH);

        private void Authenticate(string token)
        {
            if (!Authentication.IsTokenValid(token))
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
                Token = Authentication.GetToken(identityHash),
                ExpirationDate = Authentication.GetUtcCalendarDayExpiration
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

    }
}
