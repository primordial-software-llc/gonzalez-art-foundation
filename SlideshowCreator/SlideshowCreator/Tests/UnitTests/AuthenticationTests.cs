using System;
using Amazon.DynamoDBv2;
using Amazon.S3;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using NSubstitute;
using NUnit.Framework;

namespace SlideshowCreator.Tests.UnitTests
{
    public class AuthenticationTests
    {

        [Test]
        public void Authorizing_Web_Api_Paths_From_Uri()
        {
            var uri = new Uri("https://tgonzalez.net/api/Gallery/token?=REDACTED&password=REDACTED");
            Assert.AreEqual("/api/Gallery/token", uri.LocalPath);
        }

        [Test]
        public void VPC_IP()
        {
            var sampleIpFromProductionWebServer = "172.31.41.248";
            var loadBalancersCiderIpRange = "172.31.0.0/16";
            Assert.IsTrue(IPValidation.IsInSubnet(sampleIpFromProductionWebServer, loadBalancersCiderIpRange));
        }

        private string Username => "username";
        private string Password => "password";
        private string AuthoritativeSecrethash => Authentication.Hash("username:password");

        [Test]
        public void Hash()
        {
            Assert.AreEqual("vIQsManlTv4yDTDZSL5hKR887uR2bjarJfplJDzXbg4=", AuthoritativeSecrethash);
        }

        [Test]
        public void Good_Username_And_Password()
        {
            var user = new GalleryUser
            {
                TokenSalt = "EXPIRED",
                TokenSaltDate = DateTime.UtcNow.Date.AddDays(-2).Date.ToString("yyyy-MM-dd")
            };
            var userClient = Substitute.For<IDynamoDbClient<GalleryUser>>();
            userClient.Get(user).ReturnsForAnyArgs(user);

            var galleryUserAccess = new GalleryUserAccess(
                Substitute.For<IAmazonDynamoDB>(),
                new ConsoleLogging(), userClient, Substitute.For<ILogging>());
            var auth = new Authentication(galleryUserAccess);

            Assert.IsTrue(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"{Username}:{Password}")), AuthoritativeSecrethash));

            var newSalt = user.TokenSalt;
            Assert.AreNotEqual("EXPIRED", user.TokenSalt);
            Assert.AreEqual(DateTime.UtcNow.Date.ToString("yyyy-MM-dd"), user.TokenSaltDate);
            Assert.IsTrue(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"{Username}:{Password}")), AuthoritativeSecrethash)); // Verify salt doesn't change except on calendar day changes.
            Assert.AreEqual(newSalt, user.TokenSalt);

            Assert.False(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"notusername:{Password}")), AuthoritativeSecrethash));
            Assert.False(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"{Username}:notpassword")), AuthoritativeSecrethash));
        }
        
    }
}
