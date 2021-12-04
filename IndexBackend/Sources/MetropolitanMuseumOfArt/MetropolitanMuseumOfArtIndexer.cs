using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using ArtApi.Model;
using IndexBackend.Indexing;

namespace IndexBackend.Sources.MetropolitanMuseumOfArt
{
    public class MetropolitanMuseumOfArtIndexer : IIndex
    {
        public static string Source => "https://www.metmuseum.org";
        public string ImagePath => "collections/metropolitan-museum-of-art";
        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }

        public MetropolitanMuseumOfArtIndexer(HttpClient httpClient, ILogging logging)
        {
            HttpClient = httpClient;
            Logging = logging;
        }

        public async Task<IndexResult> Index(string id, ClassificationModel existing)
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
            return new IndexResult
            {
                Model = model,
                ImageJpegBytes = imageBytes
            };
        }
    }
}
