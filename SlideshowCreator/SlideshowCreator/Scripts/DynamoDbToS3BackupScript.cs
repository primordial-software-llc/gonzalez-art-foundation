using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var demoFile = archive.CreateEntry("dynamodb-backup.json", CompressionLevel.Optimal);

                    using (var entryStream = demoFile.Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        streamWriter.Write(JsonConvert.SerializeObject(allItems));
                    }
                }

                memoryStream.Position = 0;
                var bucket = "tgonzalez-dynamodb-imageclassification-backup";
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + ".zip",
                    InputStream = memoryStream
                };
                s3Client.PutObject(request);
            }

        }

    }
}
