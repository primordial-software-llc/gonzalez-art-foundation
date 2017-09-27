using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SlideshowCreator
{
    class HttpHeaders
    {

        public class Names
        {
            public const string X_CACHE = "X-Cache";
            public const string SERVER = "Server";
        }

        public class Server
        {
            public const string APACHE = "Apache";
            /// <summary>
            /// ECS is edgecast which is a CDN.
            /// https://www.cedexis.com/blog/fun-with-headers/
            /// </summary>
            public const string ECS = "ECS";
        }

        public class XCache
        {
            public const string HIT = "HIT";
        }

        public static bool HasHeader(HttpResponseHeaders headers, string name, string value)
        {
            return GetHeader(headers, name)
                .Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase));
        }

        public static List<string> GetHeader(HttpResponseHeaders headers, string headerName)
        {
            KeyValuePair<string, IEnumerable<string>> header = headers.SingleOrDefault(
                x => x.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase));

            if (header.Equals(default(KeyValuePair<string, IEnumerable<string>>)))
            {
                return new List<string>();
            }
            else
            {
                return header.Value.ToList();
            }
        }
    }
}
