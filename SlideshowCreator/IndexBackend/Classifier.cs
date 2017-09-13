using Diacritics.Extensions;

namespace IndexBackend
{
    public class Classifier
    {
        /// <summary>
        /// This is needed for the ArtistNameIndex, because keys are required.
        /// Normalize here, so that we can squash all variations e.g. "Artist not listed" or a true blank "".
        /// Amazon.DynamoDBv2.AmazonDynamoDBException : The provided key element does not match the schema
        /// </summary>
        public const string UNKNOWN_ARTIST = "unknown";

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

        private string NormalizeArtist(string artist)
        {
            if (string.IsNullOrWhiteSpace(artist))
            {
                artist = UNKNOWN_ARTIST;
            }

            artist = artist.RemoveDiacritics().ToLower();

            if (artist.Equals("artist not listed"))
            {
                artist = UNKNOWN_ARTIST;
            }

            return artist;
        }
    }
}
