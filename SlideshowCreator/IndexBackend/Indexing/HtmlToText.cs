
using HtmlAgilityPack;

namespace IndexBackend.Indexing
{
    public class HtmlToText
    {
        public static string GetText(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.InnerText;
        }
    }
}
