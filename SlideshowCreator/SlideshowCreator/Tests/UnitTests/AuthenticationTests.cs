using System;
using Amazon.S3;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using NSubstitute;
using NUnit.Framework;

namespace SlideshowCreator.Tests.UnitTests
{
    class DisclosedAuthentication
    {
        [Test]
        public void Test_Publicly_Disclosed_Authentication()
        {
            var username = "username";
            var password = "password";
            var authoritativeSecrethash = Authentication.Hash($"{username}:{password}");
            Assert.AreEqual("vIQsManlTv4yDTDZSL5hKR887uR2bjarJfplJDzXbg4=", authoritativeSecrethash);

            var user = new GalleryUser
            {
                TokenSalt = "bCw0B8BewXZ1IcMGFkJ6gEFa5OkDgvDVwIWG7dMepWY",
                TokenSaltDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd")
            };
            var userClient = Substitute.For<IDynamoDbClient<GalleryUser>>();
            userClient.Get(user).ReturnsForAnyArgs(user);
            var auth = new Authentication(Substitute.For<IAmazonS3>(), userClient);
            Assert.IsTrue(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"{username}:{password}")), authoritativeSecrethash));
            Assert.IsTrue(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"{username}:{password}")), authoritativeSecrethash)); // Verify salt doesn't change except on calendar day changes.
            Assert.False(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"notusername:{password}")), authoritativeSecrethash));
            Assert.False(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"{username}:notpassword")), authoritativeSecrethash));
        }
    }
}
