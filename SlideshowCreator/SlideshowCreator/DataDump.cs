using System;
using System.IO;
using System.Net;
using NUnit.Framework;

namespace SlideshowCreator
{
    class DataDump
    {
        public const string HTML_ARCHIVE = "C:\\Data\\HtmlArchive";
        public const string IMAGE_ARCHIVE = "C:\\Data\\ImageArchive";
        public const string CLASSIFICATION_ARCHIVE = "C:\\Data\\Classification";
        public const string FILE_IDENTITY_TEMPLATE = "page-id-";
        public const string IMAGE_RELATIVE_URL_TEMPLATE = "display_image.php?id=";

        private const int SAMPLE_PAGE_ID = 33;

        private readonly string targetUrl;
        private readonly string urlTemplate;
        private readonly string pageNotFoundIndicatorText;

        public DataDump()
        {
            
        }
        
        public DataDump(string targetUrl, string pageNotFoundIndicatorText)
        {
            this.targetUrl = targetUrl;
            urlTemplate = targetUrl + "full.php?ID={0}";
            this.pageNotFoundIndicatorText = pageNotFoundIndicatorText;
        }

        public void Dump(int pageId)
        {
            string url = string.Format(urlTemplate, pageId);
            string html;
            using (var wc = new WebClient())
            {
                html = wc.DownloadString(url);
            }
            
            if (!html.ToLower().Contains(pageNotFoundIndicatorText.ToLower()))
            {
                Persist(html, pageId);
            }
        }

        public string GetPageFileNameHtml(int pageId)
        {
            var identity = FILE_IDENTITY_TEMPLATE + pageId;
            var destinationHtml = HTML_ARCHIVE + "/" + identity + ".html";
            return destinationHtml;
        }

        public string GetPageFileNameJson(int pageId)
        {
            var identity = FILE_IDENTITY_TEMPLATE + pageId;
            var destinationHtml = CLASSIFICATION_ARCHIVE + "/" + identity + ".json";
            return destinationHtml;
        }

        public string GetBetween(string data, string start, string end)
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

        /// <remarks>
        /// Some images simply don't exist and have a link with no id.
        /// </remarks>
        public int GetImageId(string data)
        {
            string rawImageId = GetBetween(data, IMAGE_RELATIVE_URL_TEMPLATE, "\"");

            if (string.IsNullOrWhiteSpace(rawImageId))
            {
                return 0;
            }

            int imageId = int.Parse(rawImageId);
            return imageId;
        }

        private void Persist(string html, int pageId)
        {
            if (pageId == SAMPLE_PAGE_ID)
            {
                var expectedImageUrl = @"<img id=""fullimg"" src=""display_image.php?id=736170"" border=""0"" style=""display:none;"">";
                StringAssert.Contains(expectedImageUrl, html);
            }

            string destinationHtml = GetPageFileNameHtml(pageId);
            File.WriteAllText(destinationHtml, html);
            var refreshedHtml = File.ReadAllText(destinationHtml);

            if (pageId == SAMPLE_PAGE_ID)
            {
                var title = "The Mandolin Player";
                var artistsName = "Dante Gabriel Rossetti";
                var dateOfWork = "1869";
                StringAssert.Contains(title, refreshedHtml);
                StringAssert.Contains(artistsName, refreshedHtml);
                StringAssert.Contains(dateOfWork, refreshedHtml);
            }

            int imageId = GetImageId(refreshedHtml);

            if (imageId > 0)
            {
                DownloadImage(imageId, pageId);
            }
        }

        private void DownloadImage(int imageId, int pageId)
        {
            var relativeImageUrl = IMAGE_RELATIVE_URL_TEMPLATE + imageId;
            var fullImageUrl = targetUrl + relativeImageUrl;

            if (pageId == SAMPLE_PAGE_ID)
            {
                Assert.AreEqual(targetUrl + "display_image.php?id=736170", fullImageUrl);
            }

            byte[] image;
            using (var wc = new WebClient())
            {
                image = wc.DownloadData(fullImageUrl);
            }
            var destinationJpeg = IMAGE_ARCHIVE + "/" + FILE_IDENTITY_TEMPLATE + pageId + ".jpg";
            File.WriteAllBytes(destinationJpeg, image);
        }

    }
}
