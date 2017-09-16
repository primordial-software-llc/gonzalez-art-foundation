using System;
using System.Net;

namespace IndexBackend
{
    public class VpnCheck
    {
        public string IsVpnInUse(PrivateConfig privateConfig)
        {
            string html;
            using (var wc = new WebClient())
            {
                wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36");
                html = wc.DownloadString(privateConfig.IpCheckerUrl);
            }
            var expected = $@"{privateConfig.IpCheckerUrl}/ip/{privateConfig.ExpectedIp}";

            if (!html.ToLower().Contains(expected.ToLower()))
            {
                return "Expected " + expected + " but was " + html;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Try and get rid of this and avoid custom exceptions. They are hard to debug in console apps without mature logging.
        /// </summary>
        public void AssertVpnInUse(PrivateConfig privateConfig)
        {
            var inUse = IsVpnInUse(privateConfig);
            if (!string.IsNullOrWhiteSpace(inUse))
            {
                throw new Exception(inUse);
            }
        }
    }
}
