using System;
using GalleryBackend;
using Newtonsoft.Json;

namespace IndexBackend
{
    public class VpnCheck
    {
        private GalleryClient GalleryClient { get; }

        public VpnCheck(GalleryClient galleryClient)
        {
            GalleryClient = galleryClient;
        }

        public string IsVpnInUse(string secretIp)
        {
            var ipAddress = GalleryClient.GetIPAddress();

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
