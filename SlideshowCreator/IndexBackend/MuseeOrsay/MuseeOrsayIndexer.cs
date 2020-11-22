using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using Amazon.S3.Model;
using GalleryBackend.Model;
using HtmlAgilityPack;
using IndexBackend.Indexing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IndexBackend.MuseeOrsay
{
    public class MuseeOrsayIndexer : IIndex
    {
        public static string Source => "http://www.musee-orsay.fr";
        public static readonly string S3_Path = "collections/musee-orsay";
        public string S3Bucket => NationalGalleryOfArtIndexer.BUCKET + "/" + S3_Path;
        public int GetNextThrottleInMilliseconds => 0;

        private IAmazonDynamoDB DbClient { get; }
        private IAmazonS3 S3Client { get; }
        private HttpClient HttpClient { get; }

        public MuseeOrsayIndexer(IAmazonDynamoDB dbClient, IAmazonS3 s3Client, HttpClient httpClient)
        {
            DbClient = dbClient;
            S3Client = s3Client;
            HttpClient = httpClient;
        }

        public async Task<ClassificationModel> Index(string id)
        {
            var sourceLink = $"https://www.musee-orsay.fr/en/collections/index-of-works/notice.html?no_cache=1&nnumid={id}";
            var pageHtml = await HttpClient.GetStringAsync(sourceLink);
            var pageHasContent = pageHtml.Contains("corps_notice");

            if (!pageHasContent)
            {
                return null;
            }

            var model = new ClassificationModel();
            model.Source = Source;
            model.SourceLink = sourceLink;
            model.PageId = id;
            MuseeOrsayAssetDetailsParser.ParseHtmlToNewModel(pageHtml, model);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageHtml);
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
            var json = JObject.FromObject(model, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
            await DbClient.PutItemAsync(
                new ClassificationModel().GetTable(),
                Document.FromJson(json.ToString()).ToAttributeMap()
            );

            return model;
        }
    }
}
