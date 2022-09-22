using System.Text.RegularExpressions;

namespace IndexBackend.DataMaintenance
{
    public class ImageDateParsing
    {
        public static int? ParseDate(string date)
        {
            if (string.IsNullOrWhiteSpace(date))
            {
                return null;
            }
            string cleansedDate = Regex.Replace(date, @"[^\d-]", string.Empty);

            int parsedYear;
            if (int.TryParse(cleansedDate, out parsedYear))
            {
                return parsedYear;
            }

            if (cleansedDate.Contains("-"))
            {
                var splitDateRange = cleansedDate.Split("-");
                if (splitDateRange.Length > 1)
                {
                    if (int.TryParse(splitDateRange[1], out int splitParsedYear))
                    {
                        return splitParsedYear;
                    }
                }
            }
            return null;
        }
    }
}
