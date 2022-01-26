using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using ArtApi.Model;
using IndexBackend.Indexing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace IndexBackend.Sources.MuseumOfModernArt
{
    public class MuseumOfModernArtIndexer : IIndex
    {
        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }
        public static string Source => "https://www.moma.org";
        public string ImagePath => "collections/museum-of-modern-art";

        public MuseumOfModernArtIndexer(HttpClient httpClient, ILogging logging)
        {
            HttpClient = httpClient;
            Logging = logging;
        }

        public async Task<IndexResult> Index(string id, ClassificationModel existing)
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
