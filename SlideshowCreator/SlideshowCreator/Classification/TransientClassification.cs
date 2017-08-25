using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;

namespace SlideshowCreator.Classification
{
    class TransientClassification
    {
        private PrivateConfig Config { get; }

        public TransientClassification(PrivateConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Reclassify transiently.
        /// </summary>
        /// <remarks>
        /// Retries should be performed if the update fails. The method is intended to be idempotent and self-repairing. It can be run on an existing or non-existing record to produce the same result.
        /// </remarks>
        public ClassificationModel ReclassifyTransiently(int pageId)
        {
            var html = Crawler.GetDetailsPageHtml(Config.TargetUrl, pageId, Config.PageNotFoundIndicatorText);
            var classification = new Classifier().Classify(html, pageId);

            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();
            var queryRequest = new QueryRequest
            {
                TableName = request.TableName,
                KeyConditionExpression = "pageId = :v_pageId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":v_pageId", new AttributeValue { N = classification.PageId.ToString() }}}
            };
            var queryResponse = client.Query(queryRequest);
            var existingRecord = queryResponse.Items.SingleOrDefault();

            if (existingRecord != null)
            {
                existingRecord = new Dictionary<string, AttributeValue>
                {
                    {"pageId", new AttributeValue {N = existingRecord["pageId"].N}},
                    {"artist", new AttributeValue {S = existingRecord["artist"].S}}
                };
                client.DeleteItem(request.TableName, existingRecord);
            }

            var refreshedNvp = new ClassificationConversion()
                .ConvertToDynamoDb(classification);
            client.PutItem(request.TableName, refreshedNvp);

            return classification;
        }
    }
}
