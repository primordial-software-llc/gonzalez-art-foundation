
using Diacritics.Extensions;
using GalleryBackend.DataAccess;
using SlideshowCreator.InfrastructureAsCode;

namespace SlideshowCreator.Classification
{
    class Classifier
    {
        /// <summary>
        /// The images have a composite key of pageId and artist.
        /// This works well, because the pageId is unique and the artist groups related records.
        /// However, some works don't have an artist and the full composite key is required, so this placeholder must be used to avoid the error below:
        /// Amazon.DynamoDBv2.AmazonDynamoDBException : The provided key element does not match the schema
        /// </summary>
        public const string UNKNOWN_ARTIST = "Unknown";

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
