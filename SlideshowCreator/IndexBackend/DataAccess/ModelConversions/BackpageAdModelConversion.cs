using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using GalleryBackend.Model;

namespace IndexBackend.DataAccess.ModelConversions
{
    public class BackpageAdModelConversion : IModelConversion<BackpageAdModel>
    {
        public string DynamoDbTableName => BackpageAdAccess.TABLE_NAME;

        public Dictionary<string, AttributeValue> ConvertToDynamoDb(BackpageAdModel backpageAd)
        {
            var kvp = new Dictionary<string, AttributeValue>
            {
                {"source", new AttributeValue {S = backpageAd.Source}},
                {"url", new AttributeValue {S = backpageAd.Uri.AbsoluteUri}},
                {"age", new AttributeValue {N = backpageAd.Age.ToString()}},
                {"date", new AttributeValue {S = backpageAd.Date} },
                {"body", new AttributeValue {S = backpageAd.Body}}
            };
            return kvp;
        }

        public BackpageAdModel ConvertToPoco(Dictionary<string, AttributeValue> dynamoDbModel)
        {
            var model = new BackpageAdModel
            {
                Source = dynamoDbModel["source"].S,
                Uri = new Uri(dynamoDbModel["url"].S),
                Age = int.Parse(dynamoDbModel["age"].N),
                Date = dynamoDbModel["date"].S,
                Body = dynamoDbModel["body"].S
            };
            return model;
        }
    }
}
