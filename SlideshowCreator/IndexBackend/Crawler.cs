using System;

namespace IndexBackend
{
    class Crawler
    {
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
