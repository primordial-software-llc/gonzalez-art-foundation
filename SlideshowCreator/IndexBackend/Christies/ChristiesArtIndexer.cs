using System;
using System.Net.Http;
using System.Threading.Tasks;
using AwsTools;
using IndexBackend.Indexing;
using IndexBackend.Model;
using Newtonsoft.Json.Linq;

namespace IndexBackend.Christies
{
    public class ChristiesArtIndexer : IIndex
    {
        public static string Source => "https://www.christies.com";
        public string ImagePath => "collections/christies";
        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }

        public ChristiesArtIndexer(HttpClient httpClient, ILogging logging)
        {
            HttpClient = httpClient;
            Logging = logging;
        }

        public async Task<IndexResult> Index(string id)
        {
            var sourceLink = $"https://onlineonly.christies.com/s/first-open/shepard-fairey-b-1970-42/{id}";
            var htmlDoc = await new IndexingHttpClient().GetPage(HttpClient, sourceLink, Logging);
            if (htmlDoc == null)
            {
                return null;
            }
            var model = new ClassificationModel
            {
                Source = Source,
                SourceLink = sourceLink,
                PageId = id
            };
            var jsonData = Crawler.GetBetween(htmlDoc.DocumentNode.InnerHtml, "window.chrComponents =", $"{Environment.NewLine}</script>")
                .Trim()
                .TrimEnd(';')
                .Trim();
            JObject jsonDataParsed;
            try
            {
                jsonDataParsed = JObject.Parse(jsonData);
            }
            catch (Exception)
            {
                Logging.Log("Failed to parse json data in christies html on page " + sourceLink);
                Logging.Log("Failed to parse json from html: " + htmlDoc.DocumentNode.InnerHtml);
                Logging.Log("Failed to parse json: " + jsonData);
                throw;
            }
            var dataObject = jsonDataParsed["lots"]["data"]["lots"][0];
            model.Name = dataObject["title_secondary_txt"].Value<string>();
            var artistAndDate = dataObject["title_primary_txt"].Value<string>();
            if (artistAndDate.Contains("(") && artistAndDate.Contains(")"))
            {
                model.OriginalArtist = Crawler.GetBetween(artistAndDate, string.Empty, "(").Trim();
                model.Date = Crawler.GetBetween(artistAndDate, "(", ")").Trim();
            }
            else
            {
                model.OriginalArtist = artistAndDate;
                model.Date = artistAndDate;
            }
            if (dataObject["price_realised"] != null && !string.IsNullOrWhiteSpace(dataObject["price_realised"].Value<string>()))
            {
                model.Price = dataObject["price_realised"].Value<decimal>();
            }
            if (dataObject["price_realised_txt"] != null && !string.IsNullOrWhiteSpace(dataObject["price_realised_txt"].Value<string>()))
            {
                model.PriceCurrency = Crawler.GetBetween(dataObject["price_realised_txt"].Value<string>(), string.Empty, " ").Trim();
            }
            var imageUrl = dataObject["image"]["image_src"].Value<string>() ?? string.Empty;
            if (!imageUrl.StartsWith("http"))
            {
                return null;
            }
            byte[] imageBytes = null;
            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                imageBytes = await new IndexingHttpClient().GetImage(HttpClient, imageUrl, Logging);
            }
            return new IndexResult
            {
                Model = model,
                ImageBytes = imageBytes
            };
        }
    }
}
