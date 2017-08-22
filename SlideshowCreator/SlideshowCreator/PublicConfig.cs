namespace SlideshowCreator
{
    class PublicConfig
    {
        public static string DataFolder => "C:\\Data";

        public static string HtmlArchive => DataFolder + "\\HtmlArchive";

        public static string ImageArchive => DataFolder + "\\ImageArchive";

        public static string ClassificationArchive => DataFolder + "\\Classification";

        public static string DataDumpProgress => DataFolder + "\\Progress.txt";
    }
}
