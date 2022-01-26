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
        /// <remarks>Tiles aren't squares. The tiles at the max X and Y axis will be shorter.</remarks>
        public async Task<Image<Rgba64>> GetStitchedTileImageJpegBytes(string objectNumber, string apiKey)
        {
            var tilesImageRequestUrl = $"https://www.rijksmuseum.nl/api/nl/collection/{objectNumber}/tiles?key={apiKey}";
            using var httpClient = new HttpClient();
            var tilesImageResponse = await httpClient.GetStringAsync(tilesImageRequestUrl);
            var tilesImageJson = JObject.Parse(tilesImageResponse);
            if (tilesImageJson["levels"] == null || !tilesImageJson["levels"].Any())
            {
                return null;
            }
            var highestResolutionImageWidth = tilesImageJson["levels"].Max(x => x["width"].Value<int>());
            var highestResolutionImage = tilesImageJson["levels"].First(x => x["width"].Value<int>() == highestResolutionImageWidth);
            var tiles = JsonConvert.DeserializeObject<List<Tile>>(highestResolutionImage["tiles"].ToString()) ?? new List<Tile>();
            var tileImages = new List<TileImage>();
            var outputImage = new Image<Rgba64>(highestResolutionImage["width"].Value<int>(), highestResolutionImage["height"].Value<int>());
            foreach (var tile in tiles.OrderBy(x => x.Y).ThenBy(x => x.X))
            {
                using var tileClient = new HttpClient();
                var imageJpegBytes = tileClient.GetByteArrayAsync(tile.Url).Result;
                using var image = Image.Load<Rgba64>(imageJpegBytes);
                image.SaveAsJpeg(@$"C:\Users\peon\Desktop\tiles\tile-{tile.X}-{tile.Y}.jpg");
                tileImages.Add(new TileImage
                {
                    X = tile.X,
                    Y = tile.Y,
                    Height = image.Height,
                    Width = image.Width
                });
                outputImage.Mutate(o =>
                {
                    var xOffset = tileImages
                        .Where(image => image.Y == tile.Y && image.X < tile.X)
                        .Sum(x => x.Width);
                    var yOffset = tileImages
                        .Where(image => image.X == tile.X && image.Y < tile.Y)
                        .Sum(x => x.Height);
                    o.DrawImage(image, new Point(xOffset, yOffset), 1f);
                });
            }
            outputImage.SaveAsJpeg(@$"C:\Users\peon\Desktop\tiles\stitched.jpg");

            AssertImageWidthAlongAllYAxis(highestResolutionImage["width"].Value<int>(), tileImages); // Fix sporadic black blocks in stitched images even though no exceptions are thrown when getting tile data.
            AssertImageHeightAlongAllXAxis(highestResolutionImage["height"].Value<int>(), tileImages);

            return outputImage;
        }

        private void AssertImageWidthAlongAllYAxis(int width, List<TileImage> tileImages)
        {
            for (var yAxis = 0; yAxis < tileImages.Max(x => x.Y); yAxis++)
            {
                var widthForYAxis = tileImages
                    .Where(x => x.Y == yAxis)
                    .Sum(x => x.Width);
                if (widthForYAxis != width)
                {
                    throw new StitchedImageException($"Total tile width along Y axis {yAxis} of {widthForYAxis} doesn't equal image width of {width}");
                }
            }
        }

        private void AssertImageHeightAlongAllXAxis(int height, List<TileImage> tileImages)
        {
            for (var xAxis = 0; xAxis < tileImages.Max(x => x.X); xAxis++)
            {
                var heightForYAxis = tileImages
                    .Where(x => x.X == xAxis)
                    .Sum(x => x.Height);
                if (heightForYAxis != height)
                {
                    throw new StitchedImageException($"Total tile width along Y axis {xAxis} of {heightForYAxis} doesn't equal image height of {height}");
                }
            }
        }
    }
}
