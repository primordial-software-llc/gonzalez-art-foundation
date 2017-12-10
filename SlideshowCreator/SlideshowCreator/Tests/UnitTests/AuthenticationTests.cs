using GalleryBackend;
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

            var authoritativeSecrethash = Authentication.GetIdentityHash(username, password);
            Assert.AreEqual("vIQsManlTv4yDTDZSL5hKR887uR2bjarJfplJDzXbg4=", authoritativeSecrethash);
            var auth = new Authentication();

            var goodToken = auth.GetToken(Authentication.GetIdentityHash(username, password));

            Assert.IsTrue(auth.IsTokenValid(goodToken, authoritativeSecrethash));

            var badUsernameToken = auth.GetToken(Authentication.GetIdentityHash("notusername", password));
            Assert.IsFalse(auth.IsTokenValid(badUsernameToken, authoritativeSecrethash));

            var badPasswordToken = auth.GetToken(Authentication.GetIdentityHash(username, "notpassword"));
            Assert.IsFalse(auth.IsTokenValid(badPasswordToken, authoritativeSecrethash));
        }

    }
}
