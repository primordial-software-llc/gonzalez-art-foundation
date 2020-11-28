using System.Linq;
using System.Web;
using GalleryBackend.Model;
using HtmlAgilityPack;
using IndexBackend.Indexing;

namespace IndexBackend.MinistereDeLaCulture
{
    class DetailsParser
    {
        public static ClassificationModel ParseHtmlToNewModel(string source, string pageId, string sourceLink, HtmlDocument htmlDoc)
        {
            var model = new ClassificationModel {Source = source, SourceLink = sourceLink, PageId = pageId};

            var title = htmlDoc.DocumentNode
                .SelectNodes("//div[@id='Titre']/p");
            if (title != null)
            {
                model.Name = title.First().InnerText.Trim();
            }

            var artistNode = htmlDoc.DocumentNode
                .SelectNodes("//div[@id='Auteur']/p");
            var artist = string.Empty;
            if (artistNode != null)
            {
                artist = artistNode.First().InnerText.Trim();
            }
            model.Artist = Classifier.NormalizeArtist(artist);
            model.OriginalArtist = artist;

            var date = htmlDoc.DocumentNode
                .SelectNodes("//div[@id='Millésime de création']/p")
                       ?? htmlDoc.DocumentNode
                            .SelectNodes("//div[@id='Période de création']/p");
            if (date != null)
            {
                model.Date = date.First().InnerText.Trim();
            }

            return model;
        }
    }
}
