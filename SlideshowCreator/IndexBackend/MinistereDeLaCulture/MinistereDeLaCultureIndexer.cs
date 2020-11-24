using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend.Model;
using HtmlAgilityPack;
using IndexBackend.Indexing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace IndexBackend.MinistereDeLaCulture
{
    public class MinistereDeLaCultureIndexer : IIndex
    {
        public static string SourceMuseeDuLouvre => "https://www.pop.culture.gouv.fr/notice/museo/M5031";
        public static string SourceMinistereDeLaCulture => "https://www.pop.culture.gouv.fr";
        public static string S3PathLouvre => "collections/ministere-de-la-culture/louvre";
        public static string S3PathMinistereDeLaCulture => "collections/ministere-de-la-culture";
        public int GetNextThrottleInMilliseconds => 0;

        private IAmazonDynamoDB DbClient { get; }
        private IAmazonS3 S3Client { get; }
        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }
        private string Source { get; }
        private string S3Path { get; }

        public MinistereDeLaCultureIndexer(
            IAmazonDynamoDB dbClient,
            IAmazonS3 s3Client,
            HttpClient httpClient,
            ILogging logging,
            string source,
            string s3Path)
        {
            DbClient = dbClient;
            S3Client = s3Client;
            HttpClient = httpClient;
            Logging = logging;
            Source = source;
            S3Path = s3Path;
        }

        public async Task<ClassificationModel> Index(string id)
        {
            var sourceLink = $"https://www.pop.culture.gouv.fr/notice/joconde/{id}";
            var pageResponse = await HttpClient.GetAsync(sourceLink);
            string pageResponseBody = pageResponse.Content == null ? string.Empty : await pageResponse.Content.ReadAsStringAsync();
            if (!pageResponse.IsSuccessStatusCode)
            {
                Logging.Log($"Failed to GET {sourceLink}. Received {pageResponse.StatusCode}: {pageResponseBody}");
                if (pageResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }
            pageResponse.EnsureSuccessStatusCode();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageResponseBody);

            var model = DetailsParser.ParseHtmlToNewModel(Source, id, sourceLink, htmlDoc);

            var imageLinkNodes = htmlDoc.DocumentNode
                .SelectNodes("//div[@class='jsx-241519627 fieldImages']//img");

            if (imageLinkNodes == null)
            {
                return null;
            }

            var imageLink = HttpUtility.HtmlDecode(imageLinkNodes.First().Attributes["src"].Value);

            var imageResponse = await HttpClient.GetAsync(imageLink);

            if (!imageResponse.IsSuccessStatusCode)
            {
                var imageResponseBody = imageResponse.Content == null ? string.Empty : await imageResponse.Content.ReadAsStringAsync();
                Logging.Log($"Failed to GET {imageLink}. Received {imageResponse.StatusCode}: {imageResponseBody}");
                if (imageResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }
            imageResponse.EnsureSuccessStatusCode();
            var contentType = imageResponse.Content.Headers.ContentType.MediaType;
            byte[] imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();

            if (!string.Equals(contentType, "image/jpeg", StringComparison.OrdinalIgnoreCase)) // Multiple possible file extensions for a mime type and no guarantee the url has a file extension.
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
                        imageBytes = imageStream.ToArray();
                    }
                }
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

            var json = JObject.FromObject(model, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
            await DbClient.PutItemAsync(
                new ClassificationModel().GetTable(),
                Document.FromJson(json.ToString()).ToAttributeMap()
            );

            return model;
        }
    }
}
