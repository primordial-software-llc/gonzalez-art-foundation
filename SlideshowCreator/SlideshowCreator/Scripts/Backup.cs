using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GalleryBackend;
using IndexBackend;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Scripts
{
    class Backup
    {
        private readonly AmazonDynamoDBClient client = new AwsClientFactory().CreateDynamoDbClient();

        [Test]
        public void BackupTable()
        {
            var request = new CreateBackupRequest
            {
                TableName = ImageClassification.TABLE_IMAGE_CLASSIFICATION,
                BackupName = "image-classification-backup-" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ssZ")
            };
            var backupResponse = client.CreateBackup(request);
            Console.WriteLine(JsonConvert.SerializeObject(backupResponse.BackupDetails));
        }

    }
}
