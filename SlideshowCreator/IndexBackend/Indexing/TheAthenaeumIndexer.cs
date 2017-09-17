using Amazon.DynamoDBv2;

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

        public ClassificationModel Index(int id)
        {
            ClassificationModel classification = null;

            var html = Crawler.GetDetailsPageHtml(Url, id, PageNotFoundIndicatorText);

            if (!string.IsNullOrWhiteSpace(html))
            {
                var classifier = new Classifier();
                classification = classifier.ClassifyForTheAthenaeum(html, id, Source);
                var classificationConversion = new ClassificationConversion();
                var dynamoDbClassification = classificationConversion.ConvertToDynamoDb(classification);
                Client.PutItem(ImageClassificationAccess.IMAGE_CLASSIFICATION_V2, dynamoDbClassification);
            }

            return classification;
        }
    }
}
