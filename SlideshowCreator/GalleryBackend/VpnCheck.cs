using System;
using Newtonsoft.Json;

namespace GalleryBackend
{
    public class VpnCheck
    {
        private string Token { get; }

        public VpnCheck(string token)
        {
            Token = token;
        }

        public string IsVpnInUse(string secretIp)
        {
            var client = new GalleryClient();
            var ipAddress = client.GetIPAddress(Token);

            if (JsonConvert.SerializeObject(ipAddress).ToLower().Contains(secretIp))
            {
                return "Expected to not contain " + secretIp + " but was " + JsonConvert.SerializeObject(ipAddress);
            }
            else
            {
                return string.Empty;
            }
        }

        public void AssertVpnInUse(string secretIp)
        {
            var inUse = IsVpnInUse(secretIp);
            if (!string.IsNullOrWhiteSpace(inUse))
            {
                throw new Exception(inUse);
            }
        }
    }
}
