using System.Collections.Generic;
using System.Linq;
using GalleryBackend.Model;
using HtmlAgilityPack;

namespace IndexBackend.NationalGalleryOfArt
{
    public class AssetDetailsParser
    {
        /// <summary>
        /// 0 <dt>Artist</dt>
        ///   <dd>Henri Rousseau</dd>
        /// 1 <dt>Artist Info</dt>
        ///   <dd>French, 1844 - 1910</dd>
        /// 2 <dt>Title</dt>
        ///   <dd>The Equatorial Jungle</dd>
        /// 3 <dt>Dated</dt>
        ///   <dd>1909</dd>
        /// 4 <dt>Medium</dt>
        ///   <dd>oil on canvas</dd>
        /// 5 <dt>Classification</dt>
        ///   <dd>Painting</dd>
        /// 6 <dt>Dimensions</dt>
        ///   <dd>overall: 140.6 x 129.5 cm (55 3/8 x 51 in.)  framed: 151.8 x 141.3 x 6.9 cm (59 3/4 x 55 5/8 x 2 11/16 in.)</dd>
        /// 7 <dt>Credit</dt>
        ///   <dd>Chester Dale Collection</dd>
        /// 8 <dt>Accession No.</dt>
        ///   <dd>1963.10.213</dd>
        /// 9 <dt>Digitization</dt>
        ///   <dd>Direct Digital Capture</dd>
        /// 10 <dt>Image Use</dt>
        ///    <dd>Open Access</dd>
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static ClassificationModelNew ParseHtmlToNewModel(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var infoDetails = htmlDoc.DocumentNode
                .SelectNodes("//div[@id='info']/dl/dd")
                ?.Select(x => x.InnerText).ToList() ?? new List<string>();
            var infoLink = htmlDoc.DocumentNode
                                   .SelectNodes("//div[@id='info']/a")
                                   ?.FirstOrDefault()?.Attributes["href"].Value ?? string.Empty;
            
            var model = new ClassificationModelNew();
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
