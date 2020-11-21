using System;
using System.Net;
using System.Text;

namespace IndexBackend
{
    class Crawler
    {
        public const string FILE_IDENTITY_TEMPLATE = "page-id-";
        public const string IMAGE_RELATIVE_URL_TEMPLATE = "display_image.php?id=";

        public static string GetDetailsPageUrl(string targetUrl, int pageId)
        {
            return targetUrl + "full.php?ID=" + pageId;
        }

        public static string GetDetailsPageHtml(string targetUrl, int pageId, string pageNotFoundIndicatorText)
        {
            string url = GetDetailsPageUrl(targetUrl, pageId);
            string html;

            using (var wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                html = wc.DownloadString(url);
            }

            if (!html.ToLower().Contains(pageNotFoundIndicatorText.ToLower()))
            {
                return html;
            }
            else
            {
                return null;
            }
        }

        public static string GetBetween(string data, string start, string end)
        {
            var startIndex = data.IndexOf(start, StringComparison.OrdinalIgnoreCase);

            if (startIndex == -1)
            {
                return string.Empty;
            }

            var endIndex = data.IndexOf(end, startIndex, StringComparison.OrdinalIgnoreCase);

            if (startIndex == -1 || endIndex == -1)
            {
                return string.Empty;
            }

            var dataBetween = data.Substring(startIndex + start.Length, endIndex - startIndex - start.Length);
            return dataBetween;
        }

    }
}
