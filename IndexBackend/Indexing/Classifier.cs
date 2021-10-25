using System;
using Diacritics.Extensions;

namespace IndexBackend.Indexing
{
    public class Classifier
    {
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
