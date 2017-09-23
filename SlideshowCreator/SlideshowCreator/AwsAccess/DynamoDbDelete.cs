using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using IndexBackend;

namespace SlideshowCreator.AwsAccess
{
    class DynamoDbDelete
    {

        public static Dictionary<string, List<WriteRequest>> GetBatchDeletes(
            List<ClassificationModel> classifications,
            string tableName)
        {
            var batchWrite = new Dictionary<string, List<WriteRequest>> { [tableName] = new List<WriteRequest>() };

            foreach (var data in classifications)
            {
                var deleteKey = new Dictionary<string, AttributeValue>
                {
                    {"pageId", new AttributeValue {N = data.PageId.ToString()}},
                    {"artist", new AttributeValue {S = data.Artist}},
                };
                var putRequest = new DeleteRequest(deleteKey);
                var writeRequest = new WriteRequest(putRequest);
                batchWrite[tableName].Add(writeRequest);
            }

            return batchWrite;
        }
    }
}
