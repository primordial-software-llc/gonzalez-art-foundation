
using SlideshowCreator.Models;

namespace SlideshowCreator
{
    class DataClassifier
    {
        /// <summary>
        /// The images have a composite key of pageId and artist.
        /// This works well, because the pageId is unique and the artist groups related records.
        /// However, some works don't have an artist and the full composite key is required, so this placeholder must be used to avoid the error below:
        /// Amazon.DynamoDBv2.AmazonDynamoDBException : The provided key element does not match the schema
        /// </summary>
        public const string UNKNOWN_ARTIST = "Unknown";
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
