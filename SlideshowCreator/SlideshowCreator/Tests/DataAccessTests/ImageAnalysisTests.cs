using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using GalleryBackend;
using IndexBackend;
using IndexBackend.DataAccess.ModelConversions;
using IndexBackend.Indexing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class ImageAnalysisTests
    {
        private readonly IAmazonRekognition rekognitionClient = new AwsClientFactory().CreateRekognitionClientClient();
        private readonly AmazonDynamoDBClient client = new AwsClientFactory().CreateDynamoDbClient();

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

            var request = new DetectLabelsRequest
            {
                Image = new Image
                {
                    S3Object = new S3Object
                    {
                        Bucket = NationalGalleryOfArtIndexer.BUCKET,
                        Name = sampleWork.S3Path.Substring((NationalGalleryOfArtIndexer.BUCKET + "/").Length)
                    }
                }
            };

            var response = rekognitionClient.DetectLabels(request);
            Console.WriteLine(JsonConvert.SerializeObject(response));

            // The image is of a half-circle of monks using a tall device to drive poles over the river to make a bridge on a farm.
            Assert.IsTrue(response.Labels.Any(x => x.Name.Equals("Flower Arrangement")));
            Assert.IsTrue(response.Labels.Any(x => x.Name.Equals("Flower Arrangement") && x.Confidence < 70));
        }
    }
}
