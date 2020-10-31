using System;
using Diacritics.Extensions;
using GalleryBackend.Model;

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

        public ClassificationModelNew ClassifyForTheAthenaeum(string page, int pageId, string source)
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
            
            var classification = new ClassificationModelNew
            {
                Source = source,
                PageId = pageId,
                ImageId = imageId,
                Name = name,
                OriginalArtist = GetReplacementForEmptyArtist(artist),
                Artist = NormalizeArtist(artist),
                Date = date
            };

            return classification;
        }

        public static string GetReplacementForEmptyArtist(string artist)
        {
            if (string.IsNullOrWhiteSpace(artist) ||
                artist.Equals("artist not listed", StringComparison.OrdinalIgnoreCase))
            {
                artist = UNKNOWN_ARTIST;
            }

            return artist;
        }

        public static string NormalizeArtist(string artist)
        {
            artist = GetReplacementForEmptyArtist(artist);
            artist = artist.RemoveDiacritics().ToLower();
            return artist;
        }
    }
}
