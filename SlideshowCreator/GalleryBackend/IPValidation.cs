using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace GalleryBackend
{
    public class IPValidation
    {
        public const string CLOUDFLARE_IP_WHITELIST = "https://www.cloudflare.com/ips-v4";
        public const string LOAD_BALANCER_VPC = "172.31.0.0/16";

        private List<string> ipWhitelist;
        public List<string> IpWhitelist => new List<string>(ipWhitelist);
        private readonly string ipWhitelistUrl;
        private DateTime ipWhiteListLastUpdateUtc;
        private readonly TimeSpan cacheLength = new TimeSpan(hours: 0, minutes: 15, seconds: 0);

        public IPValidation(string ipWhitelistUrl)
        {
            this.ipWhitelistUrl = ipWhitelistUrl;
        }

        public bool IsInSubnet(string ipAddress)
        {
            if (DateTime.UtcNow > ipWhiteListLastUpdateUtc.Add(cacheLength))
            {
                ipWhiteListLastUpdateUtc = DateTime.UtcNow;
                ipWhitelist = GetIpWhitelist();
            }

            return IsInSubnet(ipAddress, ipWhitelist);
        }

        private List<string> GetIpWhitelist()
        {
            var ipText = new HttpClient().GetStringAsync(ipWhitelistUrl).Result;
            return ipText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .ToList();
        }

        public static bool IsInSubnet(string ipAddress, List<string> cidrList)
        {
            return cidrList.Any(cidr => IsInSubnet(ipAddress, cidr));
        }

        public static bool IsInSubnet(string ipAddress, string cidr)
        {
            string[] parts = cidr.Split('/');
            int baseAddress = BitConverter.ToInt32(IPAddress.Parse(parts[0]).GetAddressBytes(), 0);
            int address = BitConverter.ToInt32(IPAddress.Parse(ipAddress).GetAddressBytes(), 0);
            int mask = IPAddress.HostToNetworkOrder(-1 << (32 - int.Parse(parts[1])));
            return ((baseAddress & mask) == (address & mask));
        }
    }
}
