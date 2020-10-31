using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Rekognition.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.NationalGalleryOfArt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SlideshowCreator.AwsAccess;

namespace SlideshowCreator.Tests
{
    class UploadToS3
    {

        public const string BUCKET = "gonzalez-art-foundation";

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


        [Test]
        public void CreateThumbnails()
        {
            var source = "http://www.the-athenaeum.org";
            var startPageId = 0;
            var client = GalleryAwsCredentialsFactory.ProductionDbClient;
            QueryResponse queryResponse = null;
            do
            {
                var queryRequest = new QueryRequest(new ClassificationModelNew().GetTable())
                {
                    ScanIndexForward = true,
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {":source", new AttributeValue {S = source}}
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        {"#source", "source"}
                    },
                    KeyConditionExpression = "#source = :source",
                    ExclusiveStartKey = new Dictionary<string, AttributeValue>
                    {
                        {"source", new AttributeValue {S = source}},
                        {"pageId", new AttributeValue {N = startPageId.ToString()}}
                    },
                    FilterExpression = "attribute_not_exists(s3ThumbnailPath)",
                    Limit = 1000
                };
                if (queryResponse != null)
                {
                    queryRequest.ExclusiveStartKey = queryResponse.LastEvaluatedKey;
                }
                queryResponse = client.Query(queryRequest);

                if (queryResponse.Items.Any())
                {
                    var responseItems = queryResponse
                        .Items
                        .Select(item => JsonConvert.DeserializeObject<ClassificationModelNew>(Document.FromAttributeMap(item).ToJson()))
                        .ToList();

                    while (responseItems.Any())
                    {
                        var updateBatchSize = 10;
                        var batch = responseItems.Take(updateBatchSize).ToList();
                        responseItems = responseItems.Skip(updateBatchSize).ToList();

                        var updates = new List<Task>();
                        foreach (var item in batch)
                        {
                            Console.WriteLine(JsonConvert.SerializeObject(item, Formatting.Indented));
                            updates.Add(Send(item));
                        }

                        Task.WaitAll(updates.ToArray());
                    }
                }

            } while (queryResponse.LastEvaluatedKey.Any());

        }

        private async Task Send(ClassificationModelNew itemParsed)
        {
            var client = GalleryAwsCredentialsFactory.ProductionDbClient;
            var s3Client = GalleryAwsCredentialsFactory.S3AcceleratedClient;
            GetObjectResponse s3File;
            try
            {
                s3File = await s3Client.GetObjectAsync(BUCKET, itemParsed.S3Path);
            }
            catch (Exception e)
            {
                if (e.Message == "The specified key does not exist.")
                {
                    var deleted = await client.DeleteItemAsync(itemParsed.GetTable(), itemParsed.GetKey());
                    return;
                }
                else
                {
                    throw;
                }
            }

            using (var stream = s3File.ResponseStream)
            {
                var image = System.Drawing.Image.FromStream(stream);
                var thumbnailSize = ResizeKeepAspect(image.Size, 200, 200);
                var thumbnail = image.GetThumbnailImage(
                    thumbnailSize.Width,
                    thumbnailSize.Height,
                    () => false,
                    IntPtr.Zero);
                var path = $"C:\\Users\\peon\\Desktop\\thumbnails\\{Guid.NewGuid()}.jpg";
                thumbnail.Save(path);
                itemParsed.S3ThumbnailPath = $"collections/the-athenaeum/thumbnails/page-id-{itemParsed.PageId}.jpg";
                using (var fs = File.OpenRead(path))
                {
                    await s3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = "gonzalez-art-foundation",
                        Key = itemParsed.S3ThumbnailPath,
                        InputStream = fs
                    });
                }
                Dictionary<string, AttributeValue> key = itemParsed.GetKey();
                var updateJson = JObject.FromObject(itemParsed, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                foreach (var keyPart in key.Keys)
                {
                    updateJson.Remove(keyPart);
                }
                var updates = Document.FromJson(updateJson.ToString()).ToAttributeUpdateMap(false);
                await client.UpdateItemAsync(
                    itemParsed.GetTable(),
                    key,
                    updates);
            }
        }

        public static Size ResizeKeepAspect(Size src, int maxWidth, int maxHeight)
        {
            maxWidth = Math.Min(maxWidth, src.Width);
            maxHeight = Math.Min(maxHeight, src.Height);

            decimal rnd = Math.Min(maxWidth / (decimal)src.Width, maxHeight / (decimal)src.Height);
            return new Size((int)Math.Round(src.Width * rnd), (int)Math.Round(src.Height * rnd));
        }

        /// <summary>
        /// From 34478 image id
        /// </summary>
        [Test]
        public void IndexNga()
        {
            var ngaDataAccess = new NationalGalleryOfArtDataAccess(PublicConfig.NationalGalleryOfArtUri);
            var indexer = new NationalGalleryOfArtIndexer(GalleryAwsCredentialsFactory.S3AcceleratedClient, GalleryAwsCredentialsFactory.ProductionDbClient, ngaDataAccess);
            var fileIdQueueIndexer = new FileIdQueueIndexer();
            try
            {
                fileIdQueueIndexer.Index(indexer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        [Test]
        public void Move()
        {
            var devClient = new AmazonDynamoDBClient(
                CreateCredentialsTest(),
                RegionEndpoint.USEast1);

            var prodClient = GalleryAwsCredentialsFactory.ProductionDbClient;



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
                var s3AcceleratedClient = GalleryAwsCredentialsFactory.S3AcceleratedClient;

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
