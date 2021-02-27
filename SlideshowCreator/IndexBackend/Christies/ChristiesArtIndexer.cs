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
            var jsonData = Crawler.GetBetween(htmlDoc.DocumentNode.InnerHtml, "window.chrComponents =", $";{Environment.NewLine}</script>").Trim();
            var jsonDataParsed = JObject.Parse(jsonData);
            var dataObject = jsonDataParsed["lots"]["data"]["lots"][0];
            model.Name = dataObject["title_secondary_txt"].Value<string>();
            var artistAndDate = dataObject["title_primary_txt"].Value<string>();
            model.OriginalArtist = Crawler.GetBetween(artistAndDate, string.Empty, "(").Trim();
            model.Date = Crawler.GetBetween(artistAndDate, "(", ")").Trim();
            if (dataObject["price_realised"] != null)
            {
                model.Price = dataObject["price_realised"].Value<decimal>();
            }
            if (dataObject["price_realised_txt"] != null)
            {
                model.PriceCurrency = Crawler.GetBetween(dataObject["price_realised_txt"].Value<string>(), string.Empty, " ").Trim();
            }
            byte[] imageBytes = null;
            var imageUrl = dataObject["image"]["image_src"].Value<string>();
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
