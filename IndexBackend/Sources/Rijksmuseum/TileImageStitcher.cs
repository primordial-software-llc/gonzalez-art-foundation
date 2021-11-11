using System.Collections.Generic;
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
        public Image<Rgba64> GetStitchedTileImage(string objectNumber, string apiKey)
        {
            var tilesImageRequestUrl = $"https://www.rijksmuseum.nl/api/nl/collection/{objectNumber}/tiles?key={apiKey}";
            var tilesImageResponse = new HttpClient().GetStringAsync(tilesImageRequestUrl).Result;
            var tilesImageJson = JObject.Parse(tilesImageResponse);

            var highestResolutionImageWidth = tilesImageJson["levels"].Max(x => x["width"].Value<int>());
            var highestResolutionImage = tilesImageJson["levels"].First(x => x["width"].Value<int>() == highestResolutionImageWidth);

            var tiles = JsonConvert.DeserializeObject<List<Tile>>(highestResolutionImage["tiles"].ToString());

            var tileImages = new List<TileImage>();
            Image<Rgba64> stitchedImage = null;
            try
            {
                using var tileClient = new HttpClient();
                Parallel.ForEach(tiles, new ParallelOptions { MaxDegreeOfParallelism = 10 }, tile =>
                {
                    tileImages.Add(new TileImage
                    {
                        X = tile.X,
                        Y = tile.Y,
                        Image = Image.Load<Rgba64>(tileClient.GetByteArrayAsync(tile.Url).Result)
                    });
                });
                stitchedImage = StitchImages(
                    highestResolutionImage["width"].Value<int>(),
                    highestResolutionImage["height"].Value<int>(),
                    tileImages);
            }
            finally
            {
                foreach (var tileImage in tileImages)
                {
                    tileImage?.Image?.Dispose();
                }
            }
            return stitchedImage;
        }

        public Image<Rgba64> StitchImages(int width, int height, List<TileImage> tileImages)
        {
            AssertImageWidthAlongAllYAxis(width, tileImages); // Fix sporadic black blocks in stitched images even though no exceptions are thrown when getting tile data.
            AssertImageHeightAlongAllXAxis(height, tileImages);
            var outputImage = new Image<Rgba64>(width, height);
            outputImage.Mutate(o => {
                foreach (var tileImage in tileImages)
                {
                    var xOffset = tileImages
                        .Where(image => image.Y == tileImage.Y && image.X < tileImage.X)
                        .Sum(x => x.Image.Width);
                    var yOffset = tileImages
                        .Where(image => image.X == tileImage.X && image.Y < tileImage.Y)
                        .Sum(x => x.Image.Height);
                    o.DrawImage(tileImage.Image, new Point(xOffset, yOffset), 1f);
                }
            });
            return outputImage;
        }

        private void AssertImageWidthAlongAllYAxis(int width, List<TileImage> tileImages)
        {
            for (var yAxis = 0; yAxis < tileImages.Max(x => x.Y); yAxis++)
            {
                var xWidthForYAxis = tileImages
                    .Where(x => x.Y == yAxis)
                    .Sum(x => x.Image.Width);
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
                    .Sum(x => x.Image.Height);
                if (yHeightForYAxis != height)
                {
                    throw new StitchedImageException($"Total tile width along Y axis {xAxis} of {yHeightForYAxis} doesn't equal image height of {height}");
                }
            }
        }
    }
}
