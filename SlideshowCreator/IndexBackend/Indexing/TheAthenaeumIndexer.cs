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
        private PrivateConfig Config { get; }
        private AmazonDynamoDBClient Client { get; }
        private string TableName { get; }

        public TheAthenaeumIndexer()
        {
            
        }

        public TheAthenaeumIndexer(PrivateConfig config, AmazonDynamoDBClient client, string tableName)
        {
            Config = config;
            Client = client;
            TableName = tableName;
        }

        public string Source => "http://www.the-athenaeum.org";

        public ClassificationModel Index(int pageId)
        {
            ClassificationModel classification = null;

            var html = Crawler.GetDetailsPageHtml(Config.TargetUrl, pageId, Config.PageNotFoundIndicatorText);

            if (!string.IsNullOrWhiteSpace(html))
            {
                var classifier = new Classifier();
                classification = classifier.ClassifyForTheAthenaeum(html, pageId, Source);
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
