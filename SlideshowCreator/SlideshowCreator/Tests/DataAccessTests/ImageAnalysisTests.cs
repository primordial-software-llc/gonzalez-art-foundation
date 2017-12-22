using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.Indexing;
using Newtonsoft.Json;
using NUnit.Framework;
using S3Object = Amazon.Rekognition.Model.S3Object;

namespace SlideshowCreator.Tests.DataAccessTests
{
    /// <summary>
    /// http://docs.aws.amazon.com/rekognition/latest/dg/limits.html
    /// Maximum images size as raw bytes passed in as parameter to an API is 5 MB.
    /// Maximum image size stored as an Amazon S3 object is limited to 15 MB.
    /// </summary>
    class ImageAnalysisTests
    {
        private readonly IAmazonRekognition rekognitionClient = GalleryAwsCredentialsFactory.RekognitionClientClient;
        private readonly IAmazonDynamoDB client = GalleryAwsCredentialsFactory.DbClient;

        [Test]
        public void Parsing_S3_Path()
        {
            var s3Path = "tgonzalez-image-archive/national-gallery-of-art/image-80117.jpg";
            var bucket = s3Path.Substring(0, s3Path.IndexOf('/'));
            var key = s3Path.Substring(s3Path.IndexOf('/'));
            Assert.AreEqual("tgonzalez-image-archive", bucket);
            Assert.AreEqual("/national-gallery-of-art/image-80117.jpg", key);
        }

        //[Test]
        public void Normalize_Labels()
        {
            const int PAGE_SIZE = 25;
            var scanRequest = new ScanRequest(new ImageLabel().GetTable());
            var awsToolsClient = new DynamoDbClient<ImageLabel>(client, new ConsoleLogging());
            ScanResponse scanResponse = null;
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanRequest.Limit = PAGE_SIZE;
                scanResponse = client.Scan(scanRequest);
                var labels = Conversion<ImageLabel>.ConvertToPoco(scanResponse.Items);
                while (labels.Any())
                {
                    var batch = labels.Take(PAGE_SIZE).ToList();
                    labels = labels.Skip(PAGE_SIZE).ToList();
                    var failures = awsToolsClient.Insert(batch).Result;
                    labels.AddRange(failures);
                }
            } while (scanResponse.LastEvaluatedKey.Any());

        }

        //[Test]
        public void Analyze_All()
        {
            const int PAGE_SIZE = 25;
            var path = @"C:\Users\peon\Desktop\projects\SlideshowCreator\SlideshowCreator\image-analysis-progress.json";
            var scanRequest = new QueryRequest(new ClassificationModel().GetTable());
            var awsToolsClient = new DynamoDbClient<ImageLabel>(client, new ConsoleLogging());
            QueryResponse scanResponse = null;

            if (File.Exists(path))
            {
                var keyText = File.ReadAllText(path);
                var keyParsed = JsonConvert.DeserializeObject<Dictionary<string, AttributeValue>>(keyText);
                scanRequest.ExclusiveStartKey = Conversion<ClassificationModel>.ConvertToPoco(keyParsed).GetKey(); // Use a new objects for the key or the lookup will fail 
            }
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanRequest.KeyConditions = new Dictionary<string, Condition>
                {
                    {
                        "source",
                        new Condition
                        {
                            ComparisonOperator = "EQ",
                            AttributeValueList = new List<AttributeValue>
                            {
                                new AttributeValue {S = new NationalGalleryOfArtIndexer().Source}
                            }
                        }
                    }
                };
                scanRequest.Limit = PAGE_SIZE;
                scanResponse = client.Query(scanRequest);
                var images = Conversion<ClassificationModel>.ConvertToPoco(scanResponse.Items)
                    .Where(x =>
                        x.S3Path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        x.S3Path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var labels = GetLabels(images);
                while (labels.Any())
                {
                    var batch = labels.Take(PAGE_SIZE).ToList();
                    labels = labels.Skip(PAGE_SIZE).ToList();
                    var failures = awsToolsClient.Insert(batch).Result;
                    labels.AddRange(failures);
                }
                
                File.WriteAllText(path, JsonConvert.SerializeObject(scanRequest.ExclusiveStartKey));
            } while (scanResponse.LastEvaluatedKey.Any());

        }

        /// <remarks>
        /// This image is over 12MB.
        /// There is no file size limit when the image is in s3 (or it's increased).
        /// </remarks>
        [Test]
        public void Test_Image_Analysis_From_Over_5MB_File()
        {
            var sampleWorkId = 100444;
            var dbRequest = new QueryRequest(new ClassificationModel().GetTable())
            {
                KeyConditions = new Dictionary<string, Condition>
                {
                    {
                        "source",
                        new Condition
                        {
                            ComparisonOperator = "EQ",
                            AttributeValueList = new List<AttributeValue>
                            {
                                new AttributeValue {S = new NationalGalleryOfArtIndexer().Source}
                            }
                        }
                    },
                    {
                        "pageId",
                        new Condition
                        {
                            ComparisonOperator = "EQ",
                            AttributeValueList =
                                new List<AttributeValue> {new AttributeValue {N = sampleWorkId.ToString()}}
                        }
                    }
                },
                Limit = 1
            };
            var dbResponse = client.Query(dbRequest);
            var result = dbResponse.Items.Single();
            var sampleWork = Conversion<ClassificationModel>.ConvertToPoco(result);

            var label = GetLabel(sampleWork);

            Console.WriteLine(JsonConvert.SerializeObject(label));

            // The image is of a half-circle of monks using a tall device to drive poles over the river to make a bridge on a farm.
            Assert.IsTrue(label.LabelsAndConfidence.Any(x => x.StartsWith("Flower Arrangement: 5")));
        }

        private List<ImageLabel> GetLabels(List<ClassificationModel> images)
        {
            var labels = new ConcurrentBag<ImageLabel>();
            Parallel.ForEach(
                images,
                new ParallelOptions { MaxDegreeOfParallelism = 10 },
                image =>
                {
                    ImageLabel label = GetLabel(image);
                    labels.Add(label);
                });
            return labels.ToList();
        }

        private ImageLabel GetLabel(ClassificationModel image)
        {
            var request = new DetectLabelsRequest
            {
                Image = new Image
                {
                    S3Object = new S3Object
                    {
                        Bucket = NationalGalleryOfArtIndexer.BUCKET,
                        Name = image.S3Path.Substring((NationalGalleryOfArtIndexer.BUCKET + "/").Length)
                    }
                }
            };

            var response = rekognitionClient.DetectLabels(request);

            var imageLabel = new ImageLabel
            {
                Source = image.Source,
                PageId = image.PageId,
                S3Path = image.S3Path,
                LabelsAndConfidence = response
                    .Labels
                    .Select(x => x.Name + ": " + x.Confidence).ToList(),
                NormalizedLabels = response.Labels.Select(x => x.Name.ToLower()).ToList()
        };

            return imageLabel;
        }
    }
}
