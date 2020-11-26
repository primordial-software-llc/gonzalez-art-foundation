using System.Threading.Tasks;
using GalleryBackend.Model;
using IndexBackend.NormalDistributionRandom;

namespace IndexBackend.Indexing
{
    /// <summary>
    /// Reclassify transiently.
    /// </summary>
    /// <remarks>
    /// Retries should be performed if the update fails.
    /// The method is intended to be idempotent and self-repairing.
    /// It can be run on an existing or non-existing record to produce the same result.
    /// </remarks>
    public class TheAthenaeumIndexer : IIndex
    {
        public static string S3Bucket => "tgonzalez-image-archive/the-athenaeum";
        public static string Source => "http://www.the-athenaeum.org";
        public int GetNextThrottleInMilliseconds => normalizedRandom.Next();

        private readonly NormalRandomGenerator normalizedRandom = new NormalRandomGenerator(1, 1000);
        private string PageNotFoundIndicatorText { get; }
        protected virtual string Url { get; }

        public TheAthenaeumIndexer()
        {
            
        }

        public TheAthenaeumIndexer(string pageNotFoundIndicatorText, string url)
        {
            PageNotFoundIndicatorText = pageNotFoundIndicatorText;
            Url = url;
        }

        public Task<ClassificationModel> Index(string id)
        {
            var html = Crawler.GetDetailsPageHtml(Url, id, PageNotFoundIndicatorText);
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var classifier = new Classifier();
            var classification = classifier.ClassifyForTheAthenaeum(html, id, Source);

            return Task.FromResult(classification);
        }

    }
}
