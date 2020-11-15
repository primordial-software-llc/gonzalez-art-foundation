using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3.Model;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.NationalGalleryOfArt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

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
        public void CompressImage()
        {
            var source = "http://images.nga.gov";
            var pageId = 18393;
            var queryRequest = new QueryRequest(new ClassificationModelNew().GetTable())
            {
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
                    {"pageId", new AttributeValue {N = pageId.ToString()}}
                },
                Limit = 1
            };
            var client = GalleryAwsCredentialsFactory.ProductionDbClient;
            var queryResponse = client.Query(queryRequest);
            var imageItem = queryResponse
                .Items
                .Select(item =>
                    JsonConvert.DeserializeObject<ClassificationModelNew>(Document.FromAttributeMap(item).ToJson()))
                .ToList()
                .First();

            var s3Object = GalleryAwsCredentialsFactory.S3AcceleratedClient.GetObjectAsync(BUCKET, imageItem.S3Path)
                .Result;

            byte[] bytes;
            using (var stream = s3Object.ResponseStream)
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();

                const double maxMb = 5.5;
                var mb = ConvertBytesToMegabytes(bytes.Length);
                if (mb > maxMb)
                {
                    var image = Image.Load(bytes);
                    var encoder = new JpegEncoder
                    {
                        Quality = 90
                    };
                    image.Save("C:\\Users\\peon\\Desktop\\thumbnails\\test.jpg", encoder);
                }
            }
        }

        [Test]
        public void MakeThumbnail()
        {
            var source = "http://images.nga.gov";
            var pageId = 18393;
            var queryRequest = new QueryRequest(new ClassificationModelNew().GetTable())
            {
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
                    {"pageId", new AttributeValue {N = pageId.ToString()}}
                },
                Limit = 1
            };
            var client = GalleryAwsCredentialsFactory.ProductionDbClient;
            var queryResponse = client.Query(queryRequest);
            var imageItem = queryResponse
                .Items
                .Select(item =>
                    JsonConvert.DeserializeObject<ClassificationModelNew>(Document.FromAttributeMap(item).ToJson()))
                .ToList()
                .First();

            var s3Object = GalleryAwsCredentialsFactory.S3AcceleratedClient.GetObjectAsync(BUCKET, imageItem.S3Path)
                .Result;

            byte[] bytes;
            using (var stream = s3Object.ResponseStream)
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();

                var image = Image.Load(bytes);
                var newSize = ResizeKeepAspect(image.Size(), 200, 200);
                image.Mutate(x => x.Resize(newSize));
                image.Save("C:\\Users\\peon\\Desktop\\thumbnails\\test-thumbnail.jpg");

            }
        }

        static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

        private static Size ResizeKeepAspect(Size src, int maxWidth, int maxHeight)
        {
            maxWidth = Math.Min(maxWidth, src.Width);
            maxHeight = Math.Min(maxHeight, src.Height);

            decimal rnd = Math.Min(maxWidth / (decimal)src.Width, maxHeight / (decimal)src.Height);
            return new Size((int)Math.Round(src.Width * rnd), (int)Math.Round(src.Height * rnd));
        }

        [Test]
        public void CreateThumbnails()
        {
            //var source = "http://www.the-athenaeum.org";
            var source = "http://images.nga.gov";
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
                        var updateBatchSize = 3;
                        var batch = responseItems.Take(updateBatchSize).ToList();
                        responseItems = responseItems.Skip(updateBatchSize).ToList();
                        var updates = new List<Task>();
                        foreach (var item in batch)
                        {
                            Console.WriteLine(JsonConvert.SerializeObject(item, Formatting.Indented));
                            updates.Add(new ThumbnailCreator().CreateThumbnail(
                                GalleryAwsCredentialsFactory.ProductionDbClient,
                                GalleryAwsCredentialsFactory.S3AcceleratedClient,
                                BUCKET,
                                "collections/national-gallery-of-art/thumbnails/",
                                item,
                                item.S3Path == "http://images.nga.gov"));
                        }
                        try
                        {
                            Task.WaitAll(updates.ToArray());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Failed to update {updates.Count} items: {e}");
                        }
                    }
                }
            } while (queryResponse.LastEvaluatedKey.Any());
        }

        [Test]
        public void FixS3PathContainsBucket()
        {
            var source = "http://images.nga.gov";
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
                KeyConditionExpression = "#source = :source"
            };

            var results = QueryAll<ClassificationModelNew>(queryRequest, GalleryAwsCredentialsFactory.ProductionDbClient)
                .Where(x => x.S3Path.StartsWith("gonzalez-art-foundation", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Console.WriteLine($"Found {results.Count} records that need images.");

            Parallel.ForEach(results, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async result =>
            {
                var updateJson = new JObject
                {
                    { "s3Path", result.S3Path.Replace("gonzalez-art-foundation/collections/", "collections/") }
                };

                var updates = Document.FromJson(updateJson.ToString()).ToAttributeUpdateMap(false);
                var update = await GalleryAwsCredentialsFactory.ProductionDbClient.UpdateItemAsync(
                    result.GetTable(),
                    result.GetKey(),
                    updates);
                if (update.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Failed to update: " + result.PageId);
                }
                Console.WriteLine("updated " + result.PageId);
            });
        }

        [Test]
        public void IndexNga()
        {
            var source = "http://images.nga.gov";
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
                KeyConditionExpression = "#source = :source"
            };

            var results = QueryAll<ClassificationModelNew>(queryRequest, GalleryAwsCredentialsFactory.ProductionDbClient)
                .Where(x => x.S3Path.StartsWith("gonzalez-art-foundation"))
                .ToList();
            Console.WriteLine($"Found {results.Count} records that need images.");

            Parallel.ForEach(results, new ParallelOptions {MaxDegreeOfParallelism = 2}, result =>
            {
                try
                {
                    var ngaDataAccess = new NationalGalleryOfArtDataAccess(PublicConfig.NationalGalleryOfArtUri);
                    var indexer = new NationalGalleryOfArtIndexer(GalleryAwsCredentialsFactory.S3AcceleratedClient, GalleryAwsCredentialsFactory.ProductionDbClient, ngaDataAccess);
                    var classification = indexer.Index(result.PageId);
                    if (classification == null)
                    {
                        Console.WriteLine($"No image found for page id {result.PageId}");
                    }
                    else
                    {
                        Console.WriteLine($"Uploaded image for page id {result.PageId}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Thread.Sleep(1000);
                }
            });
        }

        public List<T> QueryAll<T>(QueryRequest queryRequest, IAmazonDynamoDB client, int limit = 0)
        {
            QueryResponse queryResponse = null;
            var items = new List<T>();
            do
            {
                if (queryResponse != null)
                {
                    queryRequest.ExclusiveStartKey = queryResponse.LastEvaluatedKey;
                }
                queryResponse = client.QueryAsync(queryRequest).Result;
                foreach (var item in queryResponse.Items)
                {
                    if (limit > 0 && items.Count >= limit)
                    {
                        return items;
                    }
                    items.Add(JsonConvert.DeserializeObject<T>(Document.FromAttributeMap(item).ToJson()));
                }
            } while (queryResponse.LastEvaluatedKey.Any());
            return items;
        }

        //[Test]
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
