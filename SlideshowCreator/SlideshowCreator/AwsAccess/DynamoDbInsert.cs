using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using IndexBackend;
using IndexBackend.DataAccess.ModelConversions;

namespace SlideshowCreator.AwsAccess
{
    class DynamoDbInsert
    {
        public const int BATCH_SIZE = 25;

        public static List<List<T>> Batch<T>(List<T> classifications)
        {
            return Batcher.Batch(BATCH_SIZE, classifications);
        }

        public static Dictionary<string, List<WriteRequest>> GetBatchInserts<T>(List<T> pocoModels, IModelConversion<T> modelConversion) where T : IModel, new()
        {
            var batchWrite = new Dictionary<string, List<WriteRequest>> { [modelConversion.DynamoDbTableName] = new List<WriteRequest>() };

            foreach (var pocoModel in pocoModels)
            {
                var dyamoDbModel = Conversion<T>.ConvertToDynamoDb(pocoModel);
                var putRequest = new PutRequest(dyamoDbModel);
                var writeRequest = new WriteRequest(putRequest);
                batchWrite[modelConversion.DynamoDbTableName].Add(writeRequest);
            }

            return batchWrite;
        }
    }
}
