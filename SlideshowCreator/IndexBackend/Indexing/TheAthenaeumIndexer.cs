using Amazon.DynamoDBv2;
using AwsTools;
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
        public string S3Bucket => "tgonzalez-image-archive/the-athenaeum";
        public string Source => "http://www.the-athenaeum.org";
        public string IdFileQueuePath => "C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\PageIdQueue.txt";
        public int GetNextThrottleInMilliseconds => normalizedRandom.Next();

        private readonly NormalRandomGenerator normalizedRandom = new NormalRandomGenerator(1, 1000);
        private string PageNotFoundIndicatorText { get; }
        private IAmazonDynamoDB Client { get; }
        protected virtual string Url { get; }

        public TheAthenaeumIndexer()
        {
            
        }

        public TheAthenaeumIndexer(string pageNotFoundIndicatorText, IAmazonDynamoDB client, string url)
        {
            PageNotFoundIndicatorText = pageNotFoundIndicatorText;
            Client = client;
            Url = url;
        }

        public ClassificationModelNew Index(int id)
        {
            ClassificationModelNew classification = null;

            var html = Crawler.GetDetailsPageHtml(Url, id, PageNotFoundIndicatorText);

            if (!string.IsNullOrWhiteSpace(html))
            {
                var classifier = new Classifier();
                classification = classifier.ClassifyForTheAthenaeum(html, id, Source);
                var dynamoDbClassification = Conversion<ClassificationModelNew>.ConvertToDynamoDb(classification);
                Client.PutItem(new ClassificationModel().GetTable(), dynamoDbClassification);
            }

            return classification;
        }

        public void RefreshConnection()
        {
            // Nothing to do. No keep-alives. No cookies required for access. Throttle is in use already.
        }
    }
}
