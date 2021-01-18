using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Amazon.S3;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend.Model;
using IndexBackend.Indexing;

namespace IndexBackend.MetropolitanMuseumOfArt
{
    public class MetropolitanMuseumOfArtIndexer : IIndex
    {
        public static string Source => "https://www.metmuseum.org";
        public static string S3Path => "collections/metropolitan-museum-of-art";
        private IAmazonS3 S3Client { get; }
        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }
        public int GetNextThrottleInMilliseconds => 0;


        public MetropolitanMuseumOfArtIndexer(
            IAmazonS3 s3Client,
            HttpClient httpClient,
            ILogging logging)
        {
            S3Client = s3Client;
            HttpClient = httpClient;
            Logging = logging;
        }

        public async Task<ClassificationModel> Index(string id)
        {
            var sourceLink = $"https://www.metmuseum.org/art/collection/search/{id}";
            var htmlDoc = await new IndexingHttpClient().GetPage(HttpClient, sourceLink, Logging);
            if (htmlDoc == null)
            {
                return null;
            }
            var infoNodes = htmlDoc.DocumentNode
                .SelectNodes("//section[@class='artwork-tombstone dreadfully__distinct']/p");
            var model = new ClassificationModel
            {
                Source = Source,
                SourceLink = sourceLink,
                PageId = id
            };
            if (infoNodes == null || !infoNodes.Any())
            {
                throw new Exception("Failed to find info elements, JS protection is probably in place: " + sourceLink);
            }
            var titleNode = infoNodes.Single(x => x.InnerText.ToLower().Contains("title:"));
            var titleValue = titleNode.SelectNodes(".//span[@class='artwork-tombstone--value']")
                .Single().InnerText;
            model.Name = titleValue;

            var artistNode = infoNodes.FirstOrDefault(x => x.InnerText.ToLower().Contains("artist:"));
            if (artistNode == null)
            {
                artistNode = infoNodes.First(x => x.InnerText.ToLower().Contains("culture:"));
            }
            model.OriginalArtist = artistNode.SelectNodes(".//span[@class='artwork-tombstone--value']")
                .Single().InnerText;

            var dateNode = infoNodes.FirstOrDefault(x =>
                x.InnerText.ToLower().Contains("date:"));
            if (dateNode == null)
            {
                infoNodes.FirstOrDefault(x => x.InnerText.ToLower().Contains("period:"));
            }
            if (dateNode != null)
            {
                model.Date = dateNode.SelectNodes(".//span[@class='artwork-tombstone--value']")
                    .Single().InnerText;
            }

            byte[] imageBytes = null;
            var imageLinkNodes = htmlDoc.DocumentNode.SelectNodes("//a[@class='gtm__download__image']");
            if (imageLinkNodes != null)
            {
                var imageLink = HttpUtility.HtmlDecode(imageLinkNodes.First().Attributes["href"].Value);
                if (!string.IsNullOrWhiteSpace(imageLink))
                {
                    imageBytes = await new IndexingHttpClient().GetImage(HttpClient, imageLink, Logging);
                    if (imageBytes == null)
                    {
                        imageLinkNodes = htmlDoc.DocumentNode.SelectNodes("//img[@class='artwork__image']");
                        if (imageLinkNodes != null)
                        {
                            imageLink = HttpUtility.HtmlDecode(imageLinkNodes.First().Attributes["src"].Value);
                            imageBytes = await new IndexingHttpClient().GetImage(HttpClient, imageLink, Logging);
                        }
                    }
                }
            }

            if (imageBytes == null)
            {
                return null;
            }

            using (var imageStream = new MemoryStream(imageBytes))
            {
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = NationalGalleryOfArtIndexer.BUCKET + "/" + S3Path,
                    Key = $"page-id-{id}.jpg",
                    InputStream = imageStream
                };
                await S3Client.PutObjectAsync(request);
            }

            model.S3Path = S3Path + "/" + $"page-id-{id}.jpg";

            return model;
        }
    }
}
