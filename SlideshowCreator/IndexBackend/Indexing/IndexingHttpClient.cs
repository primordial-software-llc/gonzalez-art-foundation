using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AwsTools;
using HtmlAgilityPack;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace IndexBackend.Indexing
{
    class IndexingHttpClient
    {
        public async Task<HtmlDocument> GetPage(HttpClient httpClient, string sourceLink, ILogging logging)
        {
            var pageResponse = await httpClient.GetAsync(sourceLink);
            string pageResponseBody = pageResponse.Content == null ? string.Empty : await pageResponse.Content.ReadAsStringAsync();
            if (!pageResponse.IsSuccessStatusCode && pageResponse.StatusCode != HttpStatusCode.NotFound)
            {
                logging.Log($"Failed to GET {sourceLink}. Received {pageResponse.StatusCode}: {pageResponseBody}");
            }
            if (pageResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return null; // Exit gracefully so the message isn't retried.
            }
            pageResponse.EnsureSuccessStatusCode(); // Fail hard so the message is retried.
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageResponseBody);
            return htmlDoc;
        }

        public async Task<byte[]> ConvertToJpeg(byte[] imageBytes)
        {
            using (var image = Image.Load(imageBytes))
            {
                var encoder = new JpegEncoder
                {
                    Quality = 100,
                    Subsample = JpegSubsample.Ratio444
                };
                using (var imageStream = new MemoryStream())
                {
                    await image.SaveAsync(imageStream, encoder);
                    return imageStream.ToArray();
                }
            }
        }

        public async Task<byte[]> GetImage(HttpClient httpClient, string imageLink, ILogging logging)
        {
            var imageResponse = await httpClient.GetAsync(imageLink);

            if (!imageResponse.IsSuccessStatusCode)
            {
                var imageResponseBody = imageResponse.Content == null ? string.Empty : await imageResponse.Content.ReadAsStringAsync();
                logging.Log($"Failed to GET {imageLink}. Received {imageResponse.StatusCode}: {imageResponseBody}");
                if (imageResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }
            imageResponse.EnsureSuccessStatusCode();
            var contentType = imageResponse.Content.Headers.ContentType.MediaType;
            byte[] imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
            if (!string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
            {
                imageBytes = await ConvertToJpeg(imageBytes);
            }
            return imageBytes;
        }
    }
}
