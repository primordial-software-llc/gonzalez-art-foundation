using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using GalleryBackend.Model;
using IndexBackend;
using SlideshowCreator.InfrastructureAsCode;

namespace SlideshowCreator.AwsAccess
{
    class DynamoDbInsert
    {
        public const int BATCH_SIZE = 25;

        public static List<List<ClassificationModel>> Batch(List<ClassificationModel> classifications)
        {
            List<List<ClassificationModel>> classificationBatches = new List<List<ClassificationModel>>();

            while (classifications.Any())
            {
                classificationBatches.Add(classifications.Take(BATCH_SIZE).ToList());
                classifications = classifications.Skip(BATCH_SIZE).ToList();
            }

            return classificationBatches;
        }

        public static Dictionary<string, List<WriteRequest>> GetBatchInserts(List<ClassificationModel> classifications)
        {
            var request = DynamoDbTableFactoryImageClassification.GetTableDefinition();

            var batchWrite = new Dictionary<string, List<WriteRequest>> { [request.TableName] = new List<WriteRequest>() };

            foreach (var classification in classifications)
            {
                var dyamoDbModel = new ClassificationConversion().ConvertToDynamoDb(classification);
                var putRequest = new PutRequest(dyamoDbModel);
                var writeRequest = new WriteRequest(putRequest);
                batchWrite[request.TableName].Add(writeRequest);
            }

            return batchWrite;
        }
    }
}
