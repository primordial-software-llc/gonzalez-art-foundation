using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IndexBackend.Sources.Rijksmuseum.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace IndexBackend.Sources.Rijksmuseum
{
    public class TileImageStitcher
    {
        public async Task<byte[]> GetStitchedTileImageJpegBytes(string objectNumber, string apiKey)
        {
            var tilesImageRequestUrl = $"https://www.rijksmuseum.nl/api/nl/collection/{objectNumber}/tiles?key={apiKey}";
            using var httpClient = new HttpClient();
            var tilesImageResponse = httpClient.GetStringAsync(tilesImageRequestUrl).Result;
            var tilesImageJson = JObject.Parse(tilesImageResponse);

            if (tilesImageJson["levels"] == null || !tilesImageJson["levels"].Any())
            {
                return null;
            }

            var highestResolutionImageWidth = tilesImageJson["levels"].Max(x => x["width"].Value<int>());
            var highestResolutionImage = tilesImageJson["levels"].First(x => x["width"].Value<int>() == highestResolutionImageWidth);

            var tiles = JsonConvert.DeserializeObject<List<Tile>>(highestResolutionImage["tiles"].ToString());

            var tileImages = new List<TileImage>();
            Parallel.ForEach(tiles, new ParallelOptions { MaxDegreeOfParallelism = 5 }, tile =>
            {
                using var tileClient = new HttpClient();
                var imageJpegBytes = tileClient.GetByteArrayAsync(tile.Url).Result;
                using var image = Image.Load<Rgba64>(imageJpegBytes);
                tileImages.Add(new TileImage
                {
                    X = tile.X,
                    Y = tile.Y,
                    Height = image.Height,
                    Width = image.Width,
                    ImageJpegBytes = imageJpegBytes
                });
            });
            return await StitchImages(
                highestResolutionImage["width"].Value<int>(),
                highestResolutionImage["height"].Value<int>(),
                tileImages);
        }

        public async Task<byte[]> StitchImages(int width, int height, List<TileImage> tileImages)
        {
            AssertImageWidthAlongAllYAxis(width, tileImages); // Fix sporadic black blocks in stitched images even though no exceptions are thrown when getting tile data.
            AssertImageHeightAlongAllXAxis(height, tileImages);
            using var outputImage = new Image<Rgba64>(width, height);
            outputImage.Mutate(o =>
            {
                foreach (var tileImage in tileImages)
                {
                    var xOffset = tileImages
                        .Where(image => image.Y == tileImage.Y && image.X < tileImage.X)
                        .Sum(x => x.Width);
                    var yOffset = tileImages
                        .Where(image => image.X == tileImage.X && image.Y < tileImage.Y)
                        .Sum(x => x.Height);
                    using var image = Image.Load<Rgba64>(tileImage.ImageJpegBytes);
                    o.DrawImage(image, new Point(xOffset, yOffset), 1f);
                }
            });
            await using var imageStream = new MemoryStream();
            await outputImage.SaveAsJpegAsync(imageStream);
            return imageStream.ToArray();
        }

        private void AssertImageWidthAlongAllYAxis(int width, List<TileImage> tileImages)
        {
            for (var yAxis = 0; yAxis < tileImages.Max(x => x.Y); yAxis++)
            {
                var xWidthForYAxis = tileImages
                    .Where(x => x.Y == yAxis)
                    .Sum(x => x.Width);
                if (xWidthForYAxis != width)
                {
                    throw new StitchedImageException($"Total tile width along Y axis {yAxis} of {xWidthForYAxis} doesn't equal image width of {width}");
                }
            }
        }

        private void AssertImageHeightAlongAllXAxis(int height, List<TileImage> tileImages)
        {
            for (var xAxis = 0; xAxis < tileImages.Max(x => x.Y); xAxis++)
            {
                var yHeightForYAxis = tileImages
                    .Where(x => x.X == xAxis)
                    .Sum(x => x.Height);
                if (yHeightForYAxis != height)
                {
                    throw new StitchedImageException($"Total tile width along Y axis {xAxis} of {yHeightForYAxis} doesn't equal image height of {height}");
                }
            }
        }
    }
}
