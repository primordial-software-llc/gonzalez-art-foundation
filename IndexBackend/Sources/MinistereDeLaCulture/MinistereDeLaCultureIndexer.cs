﻿using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using ArtApi.Model;
using IndexBackend.Indexing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace IndexBackend.Sources.MinistereDeLaCulture
{
    public class MinistereDeLaCultureIndexer : IIndex
    {
        public static string S3PathLouvre => "collections/ministere-de-la-culture/louvre";
        public static string S3PathMinistereDeLaCulture => "collections/ministere-de-la-culture";

        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }
        private string Source { get; }
        public string ImagePath { get; }

        public MinistereDeLaCultureIndexer(
            HttpClient httpClient,
            ILogging logging,
            string source,
            string s3Path)
        {
            HttpClient = httpClient;
            Logging = logging;
            Source = source;
            ImagePath = s3Path;
        }

        public async Task<IndexResult> Index(string id, ClassificationModel existing)
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
            return new IndexResult
            {
                Model = model,
                ImageJpeg = Image.Load<Rgba64>(imageBytes)
            };
        }

        public void Dispose()
        {
            Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
        }
    }
}
