
using Diacritics.Extensions;
using GalleryBackend.DataAccess;
using GalleryBackend.Classification;

namespace SlideshowCreator.Classification
{
    class Classifier
    {
        public static string NormalizeArtist(string artist)
        {
            return artist.RemoveDiacritics().ToLower();
        }

        public ClassificationModel ClassifyForTheAthenaeum(string page, int pageId)
        {
            var name = Crawler.GetBetween(page, "<h1>", "</h1>");
            if (name.Contains("<div"))
            {
                name = Crawler.GetBetween(page, "<h1>", "<div");
            }
            var artist = Crawler.GetBetween(page, $"The Athenaeum - {name} (", " - )");
            var date = Crawler.GetBetween(page, $"{artist}</a>", "<br/>").Trim();
            if (!string.IsNullOrWhiteSpace(date))
            {
                date = date.Substring(2, date.Length - 2);
            }
            int imageId = Crawler.GetImageId(page);

            var classification = new ClassificationModel
            {
                Source = ImageClassificationAccess.THE_ATHENAEUM,
                PageId = pageId,
                ImageId = imageId,
                Name = name,
                OriginalArtist = artist,
                Artist = NormalizeArtist(artist),
                Date = date
            };

            return classification;
        }
    }
}
