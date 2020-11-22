using System;
using Diacritics.Extensions;
using GalleryBackend.Model;

namespace IndexBackend.Indexing
{
    public class Classifier
    {
        public ClassificationModel ClassifyForTheAthenaeum(string page, string pageId, string source)
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
            
            var classification = new ClassificationModel
            {
                Source = source,
                PageId = pageId,
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
                artist = string.Empty;
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
