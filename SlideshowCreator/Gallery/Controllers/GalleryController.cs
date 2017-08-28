﻿using System;
using System.Web.Http;
using GalleryBackend;

namespace MVC5App.Controllers
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/aspnet/web-api/overview/web-api-routing-and-actions/create-a-rest-api-with-attribute-routing
    /// </summary>
    [RoutePrefix("api/Gallery")]
    public class GalleryController : ApiController
    {
        const string DISCLOSED_IDENTITY_HASH = "3vwD/tk27FM5baxW1aEh+C6DGjS7Jr5FH9/RtsuH4Lk=";
        private static readonly Authentication Authentication = new Authentication(DISCLOSED_IDENTITY_HASH);

        [Route("token")]
        public string GetAuthenticationToken(string username, string password)
        {
            var identityHash = Authentication.GetIdentityHash(username, password);
            return Authentication.GetToken(identityHash);
        }

        private void Authenticate(string token)
        {
            if (!Authentication.IsTokenValid(token))
            {
                throw new Exception("Not authenticated");
            }
        }

        [Route("searchLikeArtist")]
        public string GetLike(string token, string artist)
        {
            Authenticate(token);

            var likeJeanLeonGeromeWorks = new DynamoDbClientFactory().SearchByLikeArtist(artist);
            return likeJeanLeonGeromeWorks;
        }

        [Route("searchExactArtist")]
        public string Get(string token, string artist)
        {
            Authenticate(token);

            var jeanLeonGeromeWorks = new DynamoDbClientFactory().SearchByExactArtist(artist);
            return jeanLeonGeromeWorks;
        }
    }
}