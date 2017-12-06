using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using IndexBackend;
using IndexBackend.DataAccess;
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
                TableName = ImageClassificationAccess.TABLE_IMAGE_CLASSIFICATION,
                BackupName = "slideshowcreator-scripts-backup-" + DateTime.Now.ToString("yyyy -MM-ddTHH-mm-ssZ")
            };
            var backupResponse = client.CreateBackupAsync(request).Result;
            Console.WriteLine(JsonConvert.SerializeObject(backupResponse.BackupDetails));
        }
    }
}
