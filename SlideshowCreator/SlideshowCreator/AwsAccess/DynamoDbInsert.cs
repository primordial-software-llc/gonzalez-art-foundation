using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using IndexBackend;

namespace SlideshowCreator.AwsAccess
{
    class DynamoDbInsert
    {
        public const int BATCH_SIZE = 25;

        public static Dictionary<string, List<WriteRequest>> GetBatchInserts<T>(List<T> pocoModels) where T : IModel, new()
        {
            var batchWrite = new Dictionary<string, List<WriteRequest>> { [new T().GetTable()] = new List<WriteRequest>() };

            foreach (var pocoModel in pocoModels)
            {
                var dyamoDbModel = Conversion<T>.ConvertToDynamoDb(pocoModel);
                var putRequest = new PutRequest(dyamoDbModel);
                var writeRequest = new WriteRequest(putRequest);
                batchWrite[new T().GetTable()].Add(writeRequest);
            }

            return batchWrite;
        }
    }
}
