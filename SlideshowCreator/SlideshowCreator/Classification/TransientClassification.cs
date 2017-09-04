using Amazon.DynamoDBv2;

namespace SlideshowCreator.Classification
{
    class TransientClassification
    {
        private PrivateConfig Config { get; }
        private AmazonDynamoDBClient Client { get; }
        private string TableName { get; }

        public TransientClassification(PrivateConfig config, AmazonDynamoDBClient client, string tableName)
        {
            Config = config;
            Client = client;
            TableName = tableName;
        }

        /// <summary>
        /// Reclassify transiently.
        /// </summary>
        /// <remarks>
        /// Retries should be performed if the update fails. The method is intended to be idempotent and self-repairing. It can be run on an existing or non-existing record to produce the same result.
        /// </remarks>
        public ClassificationModel ReclassifyTheAthenaeumTransiently(int pageId)
        {
            var html = Crawler.GetDetailsPageHtml(Config.TargetUrl, pageId, Config.PageNotFoundIndicatorText);
            var classification = new Classifier().ClassifyForTheAthenaeum(html, pageId);

            var classificationConversion = new ClassificationConversion();
            var dynamoDbClassification = classificationConversion.ConvertToDynamoDb(classification);
            Client.PutItem(TableName, dynamoDbClassification);

            return classification;
        }
    }
}
