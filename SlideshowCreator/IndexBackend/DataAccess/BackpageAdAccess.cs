using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GalleryBackend.Model;

namespace IndexBackend.DataAccess
{
    public class BackpageAdAccess
    {
        public static string TABLE_NAME = "BackpageAd";
        public static string SOURCE = "http://us.backpage.com/";
        public static string INDEX_AGE = "AgeIndex";

        public void Insert(AmazonDynamoDBClient client, BackpageAdModel backpageAd)
        {
            var conversion = ConvertToDynamoDb(backpageAd);
            client.PutItem(TABLE_NAME, conversion);
        }

        public BackpageAdModel CreateModel(Uri uri, int age)
        {
            var model = new BackpageAdModel
            {
                Source = SOURCE,
                Uri = uri,
                Age = age,
                Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
            return model;
        }
        
        public Dictionary<string, AttributeValue> ConvertToDynamoDb(BackpageAdModel backpageAd)
        {
            var kvp = new Dictionary<string, AttributeValue>
            {
                {"source", new AttributeValue {S = backpageAd.Source}},
                {"url", new AttributeValue {S = backpageAd.Uri.AbsoluteUri}},
                {"age", new AttributeValue {N = backpageAd.Age.ToString()}},
                {"date", new AttributeValue {S = backpageAd.Date} }
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
                Date = dynamoDbModel["date"].S
            };
            return model;
        }

    }
}
