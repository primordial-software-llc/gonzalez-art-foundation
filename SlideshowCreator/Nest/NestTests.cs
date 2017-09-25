using System;
using System.Collections.Generic;
using System.Net.Http;
using IndexBackend;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Nest
{
    public class NestTests
    {

        [Test]
        public void Print_Nest_Secrets()
        {
            var privateConfig = PrivateConfig.CreateFromPersonalJson();
            Console.WriteLine(privateConfig.NestDecryptedAuthUrl);
        }

        [TestCase("")]
        public void CreateAuthTokenFromPin(string customerAuthPin)
        {
            // https://codelabs.developers.google.com/codelabs/wwn-api-quickstart/#4
            // 1. The user needs to go to their specific auth url page: privateConfig.NestDecryptedAuthUrl
            // 2. Then accept and get a pin and enter it as the param for this test.
            // 3. Now a token can be created from this pin one time. The token currently lasts 10 years, so this is a one-time process.

            var privateConfig = PrivateConfig.CreateFromPersonalJson();

            string responseBody;
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", privateConfig.NestDecryptedProductId),
                    new KeyValuePair<string, string>("client_secret", privateConfig.NestDecryptedProductSecret), 
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", customerAuthPin) // One time pin so it's not exactly secret after this request runs successfully and it still only is good with my specific NEST product which has all info encrypted.
                });
                HttpResponseMessage result = client.PostAsync(
                    "https://api.home.nest.com/oauth2/access_token",
                    content).Result;
                responseBody = result.Content.ReadAsStringAsync().Result;
                Console.WriteLine(responseBody);
            }
            var nestAuthResponse = JObject.Parse(responseBody);
            Assert.IsFalse(string.IsNullOrWhiteSpace(nestAuthResponse.Value<string>("access_token")));
            Assert.AreEqual(315360000, nestAuthResponse.Value<int>("expires_in"));
            // Here's what gets returned if the PIN has already been used.
            // {"error":"oauth2_error","error_description":"authorization code not found","instance_id":"8e2d9a01-7e8d-4249-b4ea-108560e65748"}
        }


    }
}
