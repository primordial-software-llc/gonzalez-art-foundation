using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.DataAccess.ModelConversions;
using IndexBackend.Indexing;
using Newtonsoft.Json;
using NUnit.Framework;
using S3Object = Amazon.Rekognition.Model.S3Object;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class ImageAnalysisTests
    {
        private readonly IAmazonRekognition rekognitionClient = new AwsClientFactory().CreateRekognitionClientClient();
        private readonly AmazonDynamoDBClient client = new AwsClientFactory().CreateDynamoDbClient();

        [Test]
        public void Run()
        {
            const int PAGE_SIZE = 25;
            var path = @"C:\Users\peon\Desktop\projects\SlideshowCreator\SlideshowCreator\image-analysis-progress.json";
            var scanRequest = new QueryRequest(ImageClassification.TABLE_IMAGE_CLASSIFICATION);
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
                scanRequest.Limit = PAGE_SIZE; // Cycle between doing a batch of image analaysis labels and inserts. Don't wait too long and slam the db then go quiet.
                scanResponse = client.Query(scanRequest);
                var images = Conversion<ClassificationModel>.ConvertToPoco(scanResponse.Items);
                var labels = images.Select(GetLabel).ToList();

                while (labels.Any())
                {
                    var batch = labels.Take(PAGE_SIZE).ToList();
                    labels = labels.Skip(PAGE_SIZE).ToList();
                    var failures = awsToolsClient.Insert(batch);
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
            var dbRequest = new QueryRequest(ImageClassification.TABLE_IMAGE_CLASSIFICATION)
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
            var sampleWork = new ClassificationConversion().ConvertToPoco(result);

            var label = GetLabel(sampleWork);

            Console.WriteLine(JsonConvert.SerializeObject(label));

            // The image is of a half-circle of monks using a tall device to drive poles over the river to make a bridge on a farm.
            Assert.IsTrue(label.Labels.Any(x => x.StartsWith("Flower Arrangement: 5")));
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
                Labels = response.Labels.Select(x => x.Name + ": " + x.Confidence).ToList() // I can't do parallel lists, because duplicates aren't allowed so I'm merging and hoping ": " isn't used in any labels.
            };

            return imageLabel;
        }
    }
}
