using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using IndexBackend.Indexing;
using IndexBackend.Model;

namespace IndexBackend.NationalGalleryOfArt
{
    public class AssetDetailsParser
    {
        public static ClassificationModel ParseHtmlToNewModel(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var infoDetails = htmlDoc.DocumentNode
                .SelectNodes("//div[@id='info']/dl/dd")
                ?.Select(x => x.InnerText).ToList() ?? new List<string>();
            var infoLink = htmlDoc.DocumentNode
                                   .SelectNodes("//div[@id='info']/a")
                                   ?.FirstOrDefault()?.Attributes["href"].Value ?? string.Empty;
            
            var model = new ClassificationModel();
            if (infoDetails.Count > 0)
            {
                model.OriginalArtist = Classifier.GetReplacementForEmptyArtist(infoDetails[0]);
                model.Artist = Classifier.NormalizeArtist(infoDetails[0]);
            }

            if (infoDetails.Count > 2)
            {
                model.Name = infoDetails[2];
            }

            if (infoDetails.Count > 3)
            {
                model.Date = infoDetails[3];
            }

            if (!string.IsNullOrWhiteSpace(infoLink))
            {
                model.SourceLink = infoLink;
            }
            
            return model;
        }
    }
}
