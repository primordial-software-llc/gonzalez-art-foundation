using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using IndexBackend.Indexing;
using IndexBackend.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace IndexBackend.Sources.MuseeOrsay
{
    public class MuseeOrsayAssetDetailsParser
    {
        public static ClassificationModel ParseHtmlToNewModel(HtmlDocument htmlDoc, ClassificationModel model)
        {
            var infoDetails = htmlDoc.DocumentNode
                .SelectNodes("//div[@class='h3_notice']")
                ?.Select(x => x.InnerText).ToList() ?? new List<string>();
            var artistName = htmlDoc.DocumentNode
                .SelectNodes("//h2")
                ?.Select(x => x.InnerText).ToList().FirstOrDefault() ?? string.Empty;
            model.OriginalArtist = artistName.Trim();
            model.Artist = Classifier.NormalizeArtist(artistName.Trim());
            if (infoDetails.Count > 0)
            {
                model.Name = infoDetails[0].Trim();
            }
            if (infoDetails.Count > 1)
            {
                model.Date = infoDetails[1].Trim();
            }
            return model;
        }

        public static async Task<byte[]> GetSmallImage(HttpClient httpClient, HtmlDocument imagePageHtmlDoc)
        {
            var lowResImgContainer = imagePageHtmlDoc
                .DocumentNode
                .SelectNodes("//div[@class='tx-damzoom-pi1']/div/div")
                .First();
            var lowResImageLinkPart1 = lowResImgContainer.Attributes["style"].Value
                .Split(';')[0]
                .Replace("background-image:url(", string.Empty)
                .Replace(")", string.Empty);
            var lowResImageLinkPart2 = lowResImgContainer.ChildNodes[0].Attributes["src"].Value;

            return await GetMergedImage(httpClient, lowResImageLinkPart1, lowResImageLinkPart2);
        }

        public static async Task<byte[]> GetLargeZoomedInImage(HttpClient httpClient, string highResImagePageLink)
        {
            var highResImagePageHtml = await httpClient.GetStringAsync(highResImagePageLink);
            var highResHtmlDoc = new HtmlDocument();
            highResHtmlDoc.LoadHtml(highResImagePageHtml);
            var highResImageLinkContainer = highResHtmlDoc.DocumentNode
                .SelectNodes("//div[@class='tx-damzoom-pi1']/div/div")
                .ToList()
                .First();
            var highResImageLinkPart1 = highResImageLinkContainer.Attributes["style"].Value
                .Split(';')[0]
                .Replace("background-image:url(", string.Empty)
                .Replace(")", string.Empty);
            var highResImageLinkPart2 = highResImageLinkContainer.ChildNodes[0].Attributes["src"].Value;
            return await GetMergedImage(httpClient, highResImageLinkPart1, highResImageLinkPart2);
        }

        public static async Task<byte[]> GetMergedImage(HttpClient httpClient, string imageLinkPart1, string imageLinkPart2)
        {
            var highResImagePart1 = await httpClient.GetByteArrayAsync(imageLinkPart1);
            var highResImagePart2 = await httpClient.GetByteArrayAsync(imageLinkPart2);
            byte[] imageBytes;
            using (var part1Image = Image.Load(highResImagePart1))
            using (var part2Image = Image.Load(highResImagePart2))
            using (var combinedImage = new Image<Rgba64>(part1Image.Width, part1Image.Height))
            {
                combinedImage.Mutate(x => x
                        .DrawImage(part1Image, new Point(0, 0), 1f)
                        .DrawImage(part2Image, new Point(0, 0), 1f) // Draw the second image over the first.
                );
                var encoder = new JpegEncoder
                {
                    Quality = 100,
                    Subsample = JpegSubsample.Ratio444
                };
                using (var imageStream = new MemoryStream())
                {
                    await combinedImage.SaveAsync(imageStream, encoder);
                    imageBytes = imageStream.ToArray();
                }
            }
            return imageBytes;
        }

    }
}
