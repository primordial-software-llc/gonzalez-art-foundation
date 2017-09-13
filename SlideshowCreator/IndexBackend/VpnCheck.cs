using System;
using System.Net;

namespace IndexBackend
{
    public class VpnCheck
    {
        public void AssertVpnInUse(PrivateConfig privateConfig)
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
                throw new Exception("Expected " + expected + " but was " + html);
            }
        }
    }
}
