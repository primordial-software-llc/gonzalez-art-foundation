using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using GalleryBackend.Model;
using IndexBackend.DataAccess;
using IndexBackend.DataAccess.ModelConversions;
using Newtonsoft.Json;

namespace IndexBackend
{
    public class DynamoDbToS3Backup
    {
        public string BackupDynamoDbTableToS3Archive(IAmazonS3 s3Client, IAmazonDynamoDB dynamoDbClient)
        {
            string bucket = "tgonzalez-dynamodb-imageclassification-backup";
            string key = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + ".zip";

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

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var backupFile = archive.CreateEntry("dynamodb-backup.json", CompressionLevel.Optimal);

                    using (var entryStream = backupFile.Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        streamWriter.Write(JsonConvert.SerializeObject(allItems));
                    }
                }

                memoryStream.Position = 0;
                PutObjectRequest request = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = key,
                    InputStream = memoryStream
                };
                s3Client.PutObject(request);
            }

            return bucket + "/" + key;
        }

    }
}
