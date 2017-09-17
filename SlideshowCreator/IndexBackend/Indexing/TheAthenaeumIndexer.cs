using System;
using System.Net;
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
        private AmazonDynamoDBClient Client { get; }
        private string TableName { get; }

        public TheAthenaeumIndexer()
        {
            
        }

        public TheAthenaeumIndexer(string pageNotFoundIndicatorText, AmazonDynamoDBClient client, string tableName)
        {
            PageNotFoundIndicatorText = pageNotFoundIndicatorText;
            Client = client;
            TableName = tableName;
        }

        public ClassificationModel Index(string url, int id)
        {
            ClassificationModel classification = null;

            var html = Crawler.GetDetailsPageHtml(url, id, PageNotFoundIndicatorText);

            if (!string.IsNullOrWhiteSpace(html))
            {
                var classifier = new Classifier();
                classification = classifier.ClassifyForTheAthenaeum(html, id, Source);
                var classificationConversion = new ClassificationConversion();
                var dynamoDbClassification = classificationConversion.ConvertToDynamoDb(classification);
                var response = Client.PutItem(TableName, dynamoDbClassification);

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("DynamoDB put failed.");
                }
            }

            return classification;
        }
    }
}
