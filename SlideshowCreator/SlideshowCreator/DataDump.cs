using System.IO;
using System.Net;
using NUnit.Framework;

namespace SlideshowCreator
{
    class DataDump
    {

        private const int SAMPLE_PAGE_ID = 33;

        private readonly string targetUrl;
        private readonly string pageNotFoundIndicatorText;

        public DataDump()
        {
            
        }
        
        public DataDump(string targetUrl, string pageNotFoundIndicatorText)
        {
            this.targetUrl = targetUrl;
            this.pageNotFoundIndicatorText = pageNotFoundIndicatorText;
        }

        public void Dump(int pageId)
        {
            var html = Crawler.GetDetailsPageHtml(targetUrl, pageId, pageNotFoundIndicatorText);
            if (!string.IsNullOrWhiteSpace(html))
            {
                Persist(html, pageId);
            }
        }

        public string GetPageFileNameHtml(int pageId)
        {
            var identity = Crawler.FILE_IDENTITY_TEMPLATE + pageId;
            var destinationHtml = PublicConfig.HtmlArchive + "/" + identity + ".html";
            return destinationHtml;
        }

        public string GetPageFileNameJson(int pageId)
        {
            var identity = Crawler.FILE_IDENTITY_TEMPLATE + pageId;
            var destinationHtml = PublicConfig.ClassificationArchive + "/" + identity + ".json";
            return destinationHtml;
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

            int imageId = Crawler.GetImageId(refreshedHtml);

            if (imageId > 0)
            {
                DownloadImage(imageId, pageId);
            }
        }

        private void DownloadImage(int imageId, int pageId)
        {
            var relativeImageUrl = Crawler.IMAGE_RELATIVE_URL_TEMPLATE + imageId;
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
            var destinationJpeg = PublicConfig.ImageArchive + "/" + Crawler.FILE_IDENTITY_TEMPLATE + pageId + ".jpg";
            File.WriteAllBytes(destinationJpeg, image);
        }

    }
}
