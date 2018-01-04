using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using AwsTools;

namespace GalleryBackend
{
    public class IPValidation
    {
        public const string CLOUDFLARE_IP_WHITELIST = "https://www.cloudflare.com/ips-v4";
        public const string LOAD_BALANCER_VPC = "172.31.0.0/16";

        private readonly string ipWhitelistUrl;
        private readonly ILogging logging;

        private List<string> ipWhitelist;
        public List<string> IpWhitelist => new List<string>(ipWhitelist);

        private DateTime ipWhiteListLastUpdateUtc;
        private readonly TimeSpan cacheLength = new TimeSpan(hours: 0, minutes: 15, seconds: 0);

        public IPValidation(string ipWhitelistUrl, ILogging logging)
        {
            this.ipWhitelistUrl = ipWhitelistUrl;
            this.logging = logging;
        }

        public bool IsInSubnet(string ipAddress)
        {
            if (DateTime.UtcNow > ipWhiteListLastUpdateUtc.Add(cacheLength)) // Probably should lock with a private static object, but no harm is done if the ip list is redundantly retrieved by parallel incoming requests and I can't even change my elastic beanstalk environment at the moment.
            {
                ipWhitelist = GetIpWhitelist();
                logging.Log($"IP whitelist changed. Last whitelist was from {ipWhiteListLastUpdateUtc:yyyy-MM-ddTHH-mm-ssZ} Latest IP's from (https://www.cloudflare.com/ips-v4): " + string.Join(",", ipWhitelist));
                ipWhiteListLastUpdateUtc = DateTime.UtcNow;
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
