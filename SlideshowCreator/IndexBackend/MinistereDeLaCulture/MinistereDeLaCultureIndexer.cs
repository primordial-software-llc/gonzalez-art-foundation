﻿using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Amazon.S3;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend.Model;
using IndexBackend.Indexing;

namespace IndexBackend.MinistereDeLaCulture
{
    public class MinistereDeLaCultureIndexer : IIndex
    {
        public static string SourceMuseeDuLouvre => "https://www.pop.culture.gouv.fr/notice/museo/M5031";
        public static string SourceMinistereDeLaCulture => "https://www.pop.culture.gouv.fr";
        public static string S3PathLouvre => "collections/ministere-de-la-culture/louvre";
        public static string S3PathMinistereDeLaCulture => "collections/ministere-de-la-culture";
        public int GetNextThrottleInMilliseconds => 0;

        private IAmazonS3 S3Client { get; }
        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }
        private string Source { get; }
        private string S3Path { get; }

        public MinistereDeLaCultureIndexer(
            IAmazonS3 s3Client,
            HttpClient httpClient,
            ILogging logging,
            string source,
            string s3Path)
        {
            S3Client = s3Client;
            HttpClient = httpClient;
            Logging = logging;
            Source = source;
            S3Path = s3Path;
        }

        public async Task<ClassificationModel> Index(string id)
        {
            var sourceLink = $"https://www.pop.culture.gouv.fr/notice/joconde/{id}";
            var htmlDoc = await new IndexingHttpClient().GetPage(HttpClient, sourceLink, Logging);
            if (htmlDoc == null)
            {
                return null;
            }

            var model = DetailsParser.ParseHtmlToNewModel(Source, id, sourceLink, htmlDoc);

            var imageLinkNodes = htmlDoc.DocumentNode
                .SelectNodes("//div[@class='jsx-241519627 fieldImages']//img");

            if (imageLinkNodes == null)
            {
                return null;
            }

            var imageLink = HttpUtility.HtmlDecode(imageLinkNodes.First().Attributes["src"].Value);
            var imageBytes = await new IndexingHttpClient().GetImage(HttpClient, imageLink, Logging);
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