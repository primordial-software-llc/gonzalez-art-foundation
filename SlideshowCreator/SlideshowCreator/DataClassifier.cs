
namespace SlideshowCreator
{
    class DataClassifier
    {
        public Classification Classify(string page)
        {
            var dataDump = new DataDump();
            var name = dataDump.GetBetween(page, "<h1>", "</h1>");
            if (name.Contains("<div"))
            {
                name = dataDump.GetBetween(page, "<h1>", "<div");
            }
            var artist = dataDump.GetBetween(page, $"The Athenaeum - {name} (", " - )");
            var date = dataDump.GetBetween(page, $"{artist}</a>", "<br/>").Trim();
            if (!string.IsNullOrWhiteSpace(date))
            {
                date = date.Substring(2, date.Length - 2);
            }
            int imageId = dataDump.GetImageId(page);

            var classification = new Classification();
            classification.Name = name;
            classification.Artist = artist;
            classification.Date = date;
            classification.ImageId = imageId;

            return classification;
        }
    }
}
