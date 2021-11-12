﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ArtApi.Model;
using IndexBackend.Indexing;
using Newtonsoft.Json.Linq;

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
                ImageJpegBytes = await new TileImageStitcher().GetStitchedTileImageJpegBytes(id, Environment.GetEnvironmentVariable("RIJKSMUSEUM_DATA_API_KEY"))
            };
            return indexResult;
        }
    }
}