using System;
using System.Net.Http;
using System.Text;
using IndexBackend;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class TpLinkTests
    {
        public static readonly string TP_LINK_API = "https://wap.tplinkcloud.com";

        class TpLinkLoginRequestParams
        {
            [JsonProperty("appType")]
            public string AppType => "Kasa_Android";

            [JsonProperty("cloudUserName")]
            public string CloudUserName { get; set;  }

            [JsonProperty("cloudPassword")]
            public string CloudPassword { get; set; }

            [JsonProperty("terminalUUID")]
            public string TerminalUuid { get; set; }
        }
        
        class TpLinkLoginRequest
        {
            [JsonProperty("method")]
            public string Method { get; set; }

            [JsonProperty("params")]
            public TpLinkLoginRequestParams Params { get; set; }
        }

        class TpLinkLoginResponseResult
        {
            [JsonProperty("accountId")]
            public string AccountId { get; set; }

            [JsonProperty("regTime")]
            public string RegTime { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("token")]
            public string Token { get; set; }
        }

        class TpLinkLoginResponse
        {
            [JsonProperty("error_code")]
            public int ErrorCode { get; set; }

            [JsonProperty("result")]
            public TpLinkLoginResponseResult Result { get; set; }
        }
        
        [Test]
        public void Test_Authentication()
        {
            var config = PrivateConfig.CreateFromPersonalJson();
            var requestParams = new TpLinkLoginRequest
            {
                Method = "login",
                Params = new TpLinkLoginRequestParams
                {
                    TerminalUuid = Guid.NewGuid().ToString(),
                    CloudUserName = config.TpLinkUsername,
                    CloudPassword = config.TpLinkPassword
                }
            };
            var request = new HttpRequestMessage(HttpMethod.Post, TP_LINK_API);
            request.Content = new StringContent(JsonConvert.SerializeObject(requestParams), Encoding.UTF8);
            var response = new HttpClient().SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            var json = response.Content.ReadAsStringAsync().Result;
            var loginResponse = JsonConvert.DeserializeObject<TpLinkLoginResponse>(json);
            Assert.AreEqual(0, loginResponse.ErrorCode);
            Assert.AreEqual(config.TpLinkUsername, loginResponse.Result.Email);
            Assert.IsNotNull(loginResponse.Result.Token);
        }
    }
}
