using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend.Model;
using HtmlAgilityPack;
using IndexBackend.Indexing;

namespace IndexBackend.MuseeOrsay
{
    public class MuseeOrsayIndexer : IIndex
    {
        public static string Source => "http://www.musee-orsay.fr";
        public static readonly string S3_Path = "collections/musee-orsay";
        public string S3Bucket => NationalGalleryOfArtIndexer.BUCKET + "/" + S3_Path;
        public int GetNextThrottleInMilliseconds => 0;

        private IAmazonS3 S3Client { get; }
        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }

        public MuseeOrsayIndexer(IAmazonS3 s3Client, HttpClient httpClient, ILogging logging)
        {
            S3Client = s3Client;
            HttpClient = httpClient;
            Logging = logging;
        }

        public async Task<ClassificationModel> Index(string id)
        {
            var sourceLink = $"https://www.musee-orsay.fr/en/collections/index-of-works/notice.html?no_cache=1&nnumid={id}";
            var htmlDoc = await new IndexingHttpClient().GetPage(HttpClient, sourceLink, Logging);
            if (htmlDoc == null)
            {
                return null;
            }
            var pageHasContent = htmlDoc.DocumentNode.OuterHtml.ToLower().Contains("corps_notice");
            if (!pageHasContent)
            {
                return null;
            }

            var model = new ClassificationModel {Source = Source, SourceLink = sourceLink, PageId = id};
            MuseeOrsayAssetDetailsParser.ParseHtmlToNewModel(htmlDoc, model);

            var imageLink = htmlDoc.DocumentNode
                .SelectNodes("//div[@class='unTiers']/a")
                ?.FirstOrDefault()?.Attributes["href"].Value ?? string.Empty;

            var imagePage = "https://www.musee-orsay.fr/" + imageLink;
            imagePage = imagePage.Replace("amp;", string.Empty);
            var imagePageHtml = await HttpClient.GetStringAsync(imagePage);

            var imagePageHtmlDoc = new HtmlDocument();
            imagePageHtmlDoc.LoadHtml(imagePageHtml);
            var highResImageLinkDiv = imagePageHtmlDoc
                .DocumentNode
                .SelectNodes("//div[@class='tx-damzoom-pi1']")
                .ToList()
                .First();
            if (highResImageLinkDiv.ChildNodes.Count < 3)
            {
                return null;
            }
            var highResImageLinkOuterContainer = highResImageLinkDiv
                .ChildNodes
                .ToList()[2];

            byte[] imageBytes;
            if (string.Equals(highResImageLinkOuterContainer.Name, "#text"))
            {
                imageBytes = await MuseeOrsayAssetDetailsParser.GetSmallImage(HttpClient, imagePageHtmlDoc);
            }
            else
            {
                var highResImageLink = highResImageLinkOuterContainer
                    .ChildNodes[0]
                    .Attributes["href"].Value
                    .Replace("amp;", string.Empty);

                var highResImageFqdn = "https://www.musee-orsay.fr/" + highResImageLink;
                imageBytes = await MuseeOrsayAssetDetailsParser.GetLargeZoomedInImage(HttpClient, highResImageFqdn);
            }


            using (var imageStream = new MemoryStream(imageBytes))
            {
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = S3Bucket,
                    Key = $"page-id-{id}.jpg",
                    InputStream = imageStream
                };
                await S3Client.PutObjectAsync(request);
            }

            model.S3Path = S3_Path + "/" + $"page-id-{id}.jpg";

            return model;
        }
    }
}
