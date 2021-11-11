using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ArtApi.Model;
using IndexBackend.Indexing;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace IndexBackend.Sources.Rijksmuseum
{
    public class RijksmuseumIndexer : IIndex
    {
        public string ImagePath => "collections/rijksmuseum";
        public static string Source => "https://www.rijksmuseum.nl";

        private HttpClient HttpClient { get; }
        private ILogging Logging { get; }

        public RijksmuseumIndexer(HttpClient httpClient, ILogging logging)
        {
            HttpClient = httpClient;
            Logging = logging;
        }

        public async Task<IndexResult> Index(string id)
        {
            var apiKey = Environment.GetEnvironmentVariable("RIJKSMUSEUM_DATA_API_KEY");
            var collectionApiRequestUrl = $"https://www.rijksmuseum.nl/api/nl/collection/{id}?key={apiKey}";
            var collectionResponse = await HttpClient.GetStringAsync(collectionApiRequestUrl);
            var collectionJson = JObject.Parse(collectionResponse);
            var model = new ClassificationModel
            {
                Source = Source,
                PageId = id,
                SourceLink = $"https://www.rijksmuseum.nl/en/collection/{id}",
                OriginalArtist = collectionJson["artObject"]["principalMaker"].Value<string>(),
                Date = collectionJson["artObject"]["dating"]["presentingDate"].Value<string>()
            };
            model.Name = collectionJson["artObject"]["titles"].Values<string>().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.Name = collectionJson["artObject"]["title"].Value<string>();
            }
            var indexResult = new IndexResult
            {
                Model = model
            };
            Image<Rgba64> stitchedImage = null;
            try
            {
                stitchedImage = new TileImageStitcher().GetStitchedTileImage(id, Environment.GetEnvironmentVariable("RIJKSMUSEUM_DATA_API_KEY"));
                await using var imageStream = new MemoryStream();
                await stitchedImage.SaveAsJpegAsync(imageStream);
                indexResult.ImageJpegBytes = imageStream.ToArray();
            }
            finally
            {
                stitchedImage?.Dispose();
            }
            return indexResult;
        }
    }
}
