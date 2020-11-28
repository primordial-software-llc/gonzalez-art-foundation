using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Amazon.S3;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend.Model;
using IndexBackend.Indexing;

namespace IndexBackend.MuseumOfModernArt
{
    public class MuseumOfModernArtIndexer : IIndex
    {
        public int GetNextThrottleInMilliseconds => 0;

        private IAmazonS3 S3Client { get; }
        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }
        public static string Source => "https://www.moma.org";
        public static string S3Path => "collections/museum-of-modern-art";

        public MuseumOfModernArtIndexer(
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
            var sourceLink = $"https://www.moma.org/collection/works/{id}";
            var htmlDoc = await new IndexingHttpClient().GetPage(HttpClient, sourceLink, Logging);
            if (htmlDoc == null)
            {
                return null;
            }

            var model = new ClassificationModel { Source = Source, SourceLink = sourceLink, PageId = id };
            var infoNodes = htmlDoc.DocumentNode
                .SelectNodes("//div[@class='work__short-caption']/h1/span");
            if (infoNodes != null && infoNodes.Count > 0)
            {
                model.OriginalArtist = infoNodes[0].InnerText.Trim();
            }
            if (infoNodes != null && infoNodes.Count > 1)
            {
                model.Name = infoNodes[1].InnerText.Trim();
            }
            if (infoNodes != null && infoNodes.Count > 2)
            {
                model.Date = infoNodes[2].InnerText.Trim();
            }

            var imageLinkNodes = htmlDoc.DocumentNode
                .SelectNodes("//img[@class='link/enable link/focus picture/image']");
            if (imageLinkNodes == null)
            {
                return null;
            }
            var imageLink = HttpUtility.HtmlDecode(imageLinkNodes.First().Attributes["data-image-overlay-src"].Value);
            var imageBytes = await new IndexingHttpClient().GetImage(HttpClient, $"https://www.moma.org{imageLink}", Logging);
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
