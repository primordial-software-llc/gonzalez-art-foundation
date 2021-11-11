using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ArtApi.Model;
using HtmlAgilityPack;
using IndexBackend.Indexing;

namespace IndexBackend.Sources.MuseeOrsay
{
    public class MuseeOrsayIndexer : IIndex
    {
        public static string Source => "http://www.musee-orsay.fr";
        public string ImagePath => "collections/musee-orsay";
        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }

        public MuseeOrsayIndexer(HttpClient httpClient, ILogging logging)
        {
            HttpClient = httpClient;
            Logging = logging;
        }

        public async Task<IndexResult> Index(string id)
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
            if (imageBytes == null)
            {
                return null;
            }
            return new IndexResult
            {
                Model = model,
                ImageJpegBytes = imageBytes
            };
        }
    }
}
