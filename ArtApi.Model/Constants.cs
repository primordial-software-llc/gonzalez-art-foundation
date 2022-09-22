
using System.Collections.Generic;

namespace ArtApi.Model
{
    public class Constants
    {
        public static readonly string IMAGES_BUCKET = "images.gonzalez-art-foundation.org";
        public static readonly string IMAGES_BUCKET_REVIEW = "gonzalez-art-foundation-review";
        public static readonly string IMAGES_BUCKET_ARCHIVE = "gonzalez-art-foundation-archive";

        public static readonly string IMAGES_TABLE = "gonzalez-art-foundation-image-classification";
        public static readonly string IMAGES_TABLE_REVIEW = "gonzalez-art-foundation-image-classification-review";
        public static readonly string IMAGES_TABLE_ARCHIVE = "gonzalez-art-foundation-image-classification-archive";

        public static readonly string ARTIST_TABLE = "gonzalez-art-foundation-artist";

        public static readonly string ORIENTATION_PORTRAIT = "portrait";
        public static readonly string ORIENTATION_LANDSCAPE = "landscape";

        public static readonly string SOURCE_THE_ATHENAEUM = "http://www.the-athenaeum.org";
        public static readonly string SOURCE_METROPOLITAN_MUSEUM_OF_ART = "https://www.metmuseum.org";
        public static readonly string SOURCE_RIJKSMUSEUM = "https://www.rijksmuseum.nl";
        public static readonly string SOURCE_MINISTERE_DE_LA_CULTURE = "https://www.pop.culture.gouv.fr";
        public static readonly string SOURCE_MUSEE_DU_LOUVRE = "https://www.pop.culture.gouv.fr/notice/museo/M5031";
        public static readonly string SOURCE_MUSEUM_OF_MODERN_ART = "https://www.moma.org";
        public static readonly string SOURCE_MUSEE_DORSAY = "http://www.musee-orsay.fr";

        public static readonly List<string> ARCHIVED_SOURCES = new List<string>
        {
            SOURCE_METROPOLITAN_MUSEUM_OF_ART,
            SOURCE_MINISTERE_DE_LA_CULTURE,
            SOURCE_MUSEE_DU_LOUVRE,
            SOURCE_MUSEUM_OF_MODERN_ART,
            SOURCE_MUSEE_DORSAY
        };
    }
}
