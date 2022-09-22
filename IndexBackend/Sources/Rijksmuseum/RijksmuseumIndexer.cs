using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ArtApi.Model;
using IndexBackend.Indexing;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;

namespace IndexBackend.Sources.Rijksmuseum
{
    public class RijksmuseumIndexer : IIndex
    {
        public string ImagePath => "collections/rijksmuseum";

        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }

        public RijksmuseumIndexer(HttpClient httpClient, ILogging logging)
        {
            HttpClient = httpClient;
            Logging = logging;
        }

        public async Task<IndexResult> Index(string id, ClassificationModel existing)
        {
            var apiKey = Environment.GetEnvironmentVariable("RIJKSMUSEUM_DATA_API_KEY");
            var collectionApiRequestUrl = $"https://www.rijksmuseum.nl/api/en/collection/{id}?key={apiKey}";
            var collectionResponse = await HttpClient.GetStringAsync(collectionApiRequestUrl);
            var collectionJson = JObject.Parse(collectionResponse);
            var model = new ClassificationModel
            {
                Source = Constants.SOURCE_RIJKSMUSEUM,
                PageId = id,
                SourceLink = $"https://www.rijksmuseum.nl/en/collection/{id}",
                OriginalArtist = collectionJson["artObject"]["principalMaker"].Value<string>(),
                Date = collectionJson["artObject"]["dating"]["presentingDate"].Value<string>(),
                Name = collectionJson["artObject"]["titles"].Values<string>().FirstOrDefault()
            };
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.Name = collectionJson["artObject"]["title"].Value<string>();
            }
            var indexResult = new IndexResult
            {
                Model = model,
                ImageJpeg = await new TileImageStitcher().GetStitchedTileImageJpegBytes(id, Environment.GetEnvironmentVariable("RIJKSMUSEUM_DATA_API_KEY"))
            };
            return indexResult;
        }

        public void Dispose()
        {
            Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
        }
    }
}
