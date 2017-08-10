using System;
using System.IO;
using System.Net;
using NUnit.Framework;

namespace SlideshowCreator
{
    class DataDump
    {
        const string HTML_ARCHIVE = "C:\\Users\\random\\Desktop\\projects\\SlideshowCreator\\HtmlArchive";
        const string IMAGE_ARCHIVE = "C:\\Users\\random\\Desktop\\projects\\SlideshowCreator\\ImageArchive";

        private readonly string targetUrl;
        private readonly string urlTemplate;
        private readonly string pageNotFoundIndicatorText;
        
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

        private void Persist(string html, int pageId)
        {
            int samplePageId = 33;
            if (pageId == samplePageId)
            {
                var expectedImageUrl = @"<img id=""fullimg"" src=""display_image.php?id=736170"" border=""0"" style=""display:none;"">";
                StringAssert.Contains(expectedImageUrl, html);
            }

            var identity = "page-id-" + pageId;
            var destinationHtml = HTML_ARCHIVE + "/" + identity + ".html";
            File.WriteAllText(destinationHtml, html);
            var refreshedHtml = File.ReadAllText(destinationHtml);

            if (pageId == samplePageId)
            {
                var title = "The Mandolin Player";
                var artistsName = "Dante Gabriel Rossetti";
                var dateOfWork = "1869";
                StringAssert.Contains(title, refreshedHtml);
                StringAssert.Contains(artistsName, refreshedHtml);
                StringAssert.Contains(dateOfWork, refreshedHtml);
            }

            var imageUrlIndex = refreshedHtml.IndexOf("display_image.php?id=", StringComparison.OrdinalIgnoreCase);
            var imageUrlEndIndex = refreshedHtml.IndexOf("\"", imageUrlIndex, StringComparison.OrdinalIgnoreCase);

            var relativeImageUrl = refreshedHtml.Substring(imageUrlIndex, imageUrlEndIndex - imageUrlIndex);
            var fullImageUrl = targetUrl + relativeImageUrl;

            if (pageId == samplePageId)
            {
                Assert.AreEqual(targetUrl + "display_image.php?id=736170", fullImageUrl);
            }

            byte[] image;
            using (var wc = new WebClient())
            {
                image = wc.DownloadData(fullImageUrl);
            }
            var destinationJpeg = IMAGE_ARCHIVE + "/" + identity + ".jpg";
            File.WriteAllBytes(destinationJpeg, image);
        }

    }
}
