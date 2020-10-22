using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using GalleryBackend.Model;
using Newtonsoft.Json;

namespace SlideshowCreator.AwsAccess
{
    class DynamoDbInsert
    {
        public const int BATCH_SIZE = 25;

        public static Dictionary<string, List<WriteRequest>> GetBatchInserts<T>(List<T> pocoModels) where T : IModel, new()
        {
            var batchWrite = new Dictionary<string, List<WriteRequest>> { [new ClassificationModelNew().GetTable()] = new List<WriteRequest>() };

            foreach (var pocoModel in pocoModels)
            {
                var dyamoDbModel = Document.FromJson(JsonConvert.SerializeObject(pocoModel, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })).ToAttributeMap();
                var putRequest = new PutRequest(dyamoDbModel);
                var writeRequest = new WriteRequest(putRequest);
                batchWrite[new ClassificationModelNew().GetTable()].Add(writeRequest);
            }

            return batchWrite;
        }
    }
}
