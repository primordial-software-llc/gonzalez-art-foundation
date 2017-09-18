using System.Xml;

namespace IndexBackend
{
    public class XmlDocFactory
    {
        /// <summary>
        /// https://stackoverflow.com/questions/56107/what-is-the-best-way-to-parse-html-in-c
        /// I'm highly cautious of what's on the web.
        /// Hopefully this should give you an idea of how far I want to take this.
        /// Eventually I believe I will face agressive defenders.
        /// I want to take it slow. There's no rush, I'm moving quickly already.
        /// 
        /// And even though images.nga.gov has an xml header, it doesn't close the <img> tags so it's not valid xml. Idk, I need to review the XmlAgilityPack if I crawl more.
        /// </summary>
        /// <remarks>
        /// Xml Resolver is set to null in order to prevent XXE attacks:
        /// https://stackoverflow.com/questions/14230988/how-to-prevent-xxe-attack-xmldocument-in-net
        /// </remarks>
        public static XmlDocument Create(string xml)
        {
            XmlDocument xmlDoc = new XmlDocument { XmlResolver = null };
            xmlDoc.LoadXml(xml);
            return xmlDoc;
        }
    }
}
