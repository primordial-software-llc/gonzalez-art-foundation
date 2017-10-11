using System;
using Amazon.DynamoDBv2;
using GalleryBackend.Model;
using IndexBackend.DataAccess.ModelConversions;

namespace IndexBackend.DataAccess
{
    public class BackpageAdAccess
    {
        public static string TABLE_NAME = "BackpageAd";
        public static string SOURCE = "http://us.backpage.com/";
        public static string INDEX_AGE = "AgeIndex";

        public void Insert(AmazonDynamoDBClient client, BackpageAdModel backpageAd)
        {
            var conversion = new BackpageAdModelConversion().ConvertToDynamoDb(backpageAd);
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

    }
}
