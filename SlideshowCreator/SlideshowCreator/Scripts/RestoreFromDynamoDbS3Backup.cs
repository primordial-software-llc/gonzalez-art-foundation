using System.Collections.Generic;
using System.IO;
using System.Linq;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Scripts
{
    class RestoreFromDynamoDbS3Backup
    {

        [Test]
        public void Restore()
        {
            var path = @"C:\Users\peon\Desktop\projects\SlideshowCreator\SlideshowCreator\dynamodb-backup.json";
            var json = File.ReadAllText(path);
            var models = JsonConvert.DeserializeObject<List<ClassificationModelNew>>(json);

            var client = new DynamoDbClient<ClassificationModelNew>(GalleryAwsCredentialsFactory.ProductionDbClient, new ConsoleLogging());

            // This batching should go into the client. 25 is the max allowed.
            while (models.Any())
            {
                var batch = models.Take(25).ToList();
                models = models.Skip(25).ToList();
                models.AddRange(client.Insert(batch).Result);
                File.WriteAllText(path, JsonConvert.SerializeObject(models));
            }
        }
    }
}
