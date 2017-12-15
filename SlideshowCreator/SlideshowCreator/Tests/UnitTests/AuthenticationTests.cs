using System;
using GalleryBackend;
using NUnit.Framework;

namespace SlideshowCreator.Tests.UnitTests
{
    class DisclosedAuthentication
    {
        private class AuthenticationExposed : Authentication
        {
            public AuthenticationExposed(string salt)
            {
                salteDate = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
                base64RandomBytes = salt;
            }
        }

        [Test]
        public void Test_Publicly_Disclosed_Authentication()
        {
            var username = "username";
            var password = "password";
            var salt = "bCw0B8BewXZ1IcMGFkJ6gEFa5OkDgvDVwIWG7dMepWY=";
            var authoritativeSecrethash = Authentication.Hash($"{username}:{password}");
            Assert.AreEqual("vIQsManlTv4yDTDZSL5hKR887uR2bjarJfplJDzXbg4=", authoritativeSecrethash);

            var auth = new AuthenticationExposed(salt);
            Assert.IsTrue(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"{username}:{password}")), authoritativeSecrethash));
            Assert.IsTrue(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"{username}:{password}")), authoritativeSecrethash)); // Verify salt doesn't change except on calendar day changes.
            Assert.False(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"notusername:{password}")), authoritativeSecrethash));
            Assert.False(auth.IsTokenValid(auth.GetToken(Authentication.Hash($"{username}:notpassword")), authoritativeSecrethash));
        }
    }
}
