using System;

namespace IndexBackend
{
    public class PublicConfig
    {
        public static string DataFolder => "C:\\Data";

        public static string HtmlArchive => DataFolder + "\\HtmlArchive";

        public static string ImageArchive => DataFolder + "\\ImageArchive";

        public static string ClassificationArchive => DataFolder + "\\Classification";

        public static string DataDumpProgress => DataFolder + "\\Progress.txt";

        public static string TheAthenaeumArt => "http://www.the-athenaeum.org/art/";

        public static Uri NationalGalleryOfArtUri => new Uri("http://images.nga.gov/en/page/show_home_page.html");
    }
}
