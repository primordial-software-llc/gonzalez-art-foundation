using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using GalleryBackend.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using SlideshowCreator.AwsAccess;

namespace SlideshowCreator.Tests
{
    class UploadToS3
    {

        public static RegionEndpoint HomeRegion => RegionEndpoint.USEast1;
        public static AWSCredentials CreateCredentials()
        {
            var chain = new CredentialProfileStoreChain();
            var profile = "gonzalez-art-foundation";
            if (!chain.TryGetAWSCredentials(profile, out AWSCredentials awsCredentials))
            {
                throw new Exception($"AWS credentials not found for \"{profile}\" profile.");
            }
            return awsCredentials;
        }

        public static AWSCredentials CreateCredentialsTest()
        {
            var chain = new CredentialProfileStoreChain();
            var profile = "test";
            if (!chain.TryGetAWSCredentials(profile, out AWSCredentials awsCredentials))
            {
                throw new Exception($"AWS credentials not found for \"{profile}\" profile.");
            }
            return awsCredentials;
        }

        public List<T> ScanAll<T>(AmazonDynamoDBClient client, ScanRequest scanRequest)
        {
            ScanResponse scanResponse = null;
            var items = new List<T>();
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanResponse = client.ScanAsync(scanRequest).Result;
                foreach (var item in scanResponse.Items)
                {
                    items.Add(JsonConvert.DeserializeObject<T>(Document.FromAttributeMap(item).ToJson()));
                }
            } while (scanResponse.LastEvaluatedKey.Any());
            return items;
        }

        [Test]
        public void DeleteInvalid()
        {
            var prodClient = new AmazonDynamoDBClient(
                CreateCredentials(),
                RegionEndpoint.USEast1);
            ScanResponse scanResponse = null;
            do
            {
                var scanRequest = new ScanRequest(new ClassificationModelNew().GetTable()) {Limit = 1000};
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanResponse = prodClient.ScanAsync(scanRequest).Result;
                var deletes = new List<Task<DeleteItemResponse>>();
                foreach (var item in scanResponse.Items)
                {
                    var itemParsed = JsonConvert.DeserializeObject<ClassificationModelNew>(Document.FromAttributeMap(item).ToJson());
                    if (itemParsed.Source.StartsWith("collection", StringComparison.OrdinalIgnoreCase))
                    {
                        deletes.Add(prodClient.DeleteItemAsync(new ClassificationModelNew().GetTable(), itemParsed.GetKey()));
                    }
                }
                Task.WaitAll(deletes.ToArray());
            } while (scanResponse.LastEvaluatedKey.Any());
        }

        [Test]
        public void Move()
        {
            var devClient = new AmazonDynamoDBClient(
                CreateCredentialsTest(),
                RegionEndpoint.USEast1);

            var prodClient = new AmazonDynamoDBClient(
                CreateCredentials(),
                RegionEndpoint.USEast1);



            ScanResponse scanResponse = null;
            do
            {
                var scanRequest = new ScanRequest(new ClassificationModel().GetTable()) {Limit = 25};
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanResponse = devClient.ScanAsync(scanRequest).Result;
                var items = new List<ClassificationModel>();
                foreach (var item in scanResponse.Items)
                {
                    var itemParsed = JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(item).ToJson());

                    if (string.Equals(itemParsed.Artist, "unknown", StringComparison.OrdinalIgnoreCase))
                    {
                        itemParsed.Artist = null;
                    }

                    if (string.Equals(itemParsed.Source, "http://images.nga.gov", StringComparison.OrdinalIgnoreCase))
                    {
                        itemParsed.S3Path = itemParsed.Source.Replace(
                            "tgonzalez-image-archive/national-gallery-of-art/",
                            "gonzalez-art-foundation/collections/national-gallery-of-art/");
                    }
                    else if (string.Equals(itemParsed.Source, "http://www.the-athenaeum.org", StringComparison.OrdinalIgnoreCase))
                    {
                        itemParsed.S3Path = $"collections/the-athenaeum/page-id-{itemParsed.PageId}.jpg";
                    }
                    items.Add(itemParsed);
                }

                var inserts = DynamoDbInsert.GetBatchInserts(items);
                var insertResults = prodClient.BatchWriteItem(inserts);

                if (insertResults.UnprocessedItems.Any())
                {
                    throw new Exception("Failed to process items");
                }

            } while (scanResponse.LastEvaluatedKey.Any());
        }

        [Test]
        public void SendToS3()
        {
            var files = Directory.GetFiles("G:\\Data\\ImageArchive")
                .Where(x => !string.Equals(x, "G:\\Data\\ImageArchive\\desktop.ini"))
                .ToList();

            foreach (var file in files)
            {
                var fileName = file.Split('\\').Last();
                Console.WriteLine(fileName);
                var s3AcceleratedClient = new AmazonS3Client(
                    CreateCredentials(),
                    new AmazonS3Config
                    {
                        RegionEndpoint = RegionEndpoint.USEast1,
                        UseAccelerateEndpoint = true
                    });

                using (FileStream fs = File.OpenRead(file))
                {
                    s3AcceleratedClient.PutObject(new PutObjectRequest
                    {
                        BucketName = "gonzalez-art-foundation/collections/the-athenaeum",
                        Key = fileName,
                        InputStream = fs
                    });
                }
            }

        }
    }
}
