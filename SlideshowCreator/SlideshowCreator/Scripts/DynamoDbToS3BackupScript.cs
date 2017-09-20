using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using IndexBackend;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Scripts
{
    class DynamoDbToS3BackupScript
    {
        readonly IAmazonS3 s3Client = new AwsClientFactory().CreateS3Client();
        readonly IAmazonDynamoDB dynamoDbClient = new AwsClientFactory().CreateDynamoDbClient();

        [Test]
        public void A_Write_Backup_Keys_To_File()
        {
            
            var scanRequest = new ScanRequest(ImageClassificationAccess.IMAGE_CLASSIFICATION_V2);

            var allItems = new List<ClassificationModel>();
            var conversion = new ClassificationConversion();

            ScanResponse scanResponse = null;
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }

                scanResponse = dynamoDbClient.Scan(scanRequest);
                allItems.AddRange(conversion.ConvertToPoco(scanResponse.Items));
            } while (scanResponse.LastEvaluatedKey.Any());

            Assert.IsNotNull(allItems.First().Source);
            Assert.Greater(allItems.First().PageId, 0);

            var expectedItems = 292105;
            Assert.AreEqual(expectedItems, allItems.Count);

            var jsonItems = (from i in allItems select JsonConvert.SerializeObject(i)).ToList();
            File.WriteAllLines("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\DynamoDbToS3Backup.json", jsonItems);
        }

        [Test]
        public void B_Check_Counts_From_Read()
        {
            var rawItems = File.ReadAllLines("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\DynamoDbToS3Backup.json");
            var expectedItems = 292105;
            Assert.AreEqual(expectedItems, rawItems.Count());

            var allItems = (from i in rawItems select JsonConvert.DeserializeObject<ClassificationModel>(i)).ToList();
            Assert.AreEqual("http://www.the-athenaeum.org", allItems.First().Source);
            Assert.AreEqual(33, allItems.First().PageId);
        }

        [Test]
        public void C_Sync_To_S3()
        {
            var bucket = "tgonzalez-dynamodb-imageclassification-backup/" + DateTime.UtcNow.ToString("yyyy-MM-dd");
            Regex urlRegex = new Regex("[^a-zA-Z0-9 -]");
            var rawItems = File.ReadAllLines("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\DynamoDbToS3Backup.json").ToList();

            for (int i = rawItems.Count - 1; i > -1; i--)
            {
                var parsedItem = JsonConvert.DeserializeObject<ClassificationModel>(rawItems[i]);
                var key = urlRegex.Replace(parsedItem.Source, string.Empty) + "-" + parsedItem.PageId;

                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    ContentBody = rawItems[i]
                };
                s3Client.PutObject(request); // S3 has no bulk operation. Could go in paralle, but it woudl require a more complex thread-safe queue. Still getting core infrastructure and processes up and running. Can't start adding fancy SQS thread safe queues yet. Potentially in the future.

                rawItems.RemoveAt(i);
                File.WriteAllLines("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\DynamoDbToS3Backup.json", rawItems); // SSD is  must for this project.
            }
        }
    }
}
