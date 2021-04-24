using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using IndexBackend;
using IndexBackend.Model;
using IndexBackend.Sources.NationalGalleryOfArt;
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
        private readonly IAmazonDynamoDB client = GalleryAwsCredentialsFactory.ProductionDbClient;

        [Test]
        public void Parsing_S3_Path()
        {
            var s3Path = "tgonzalez-image-archive/national-gallery-of-art/image-80117.jpg";
            var bucket = s3Path.Substring(0, s3Path.IndexOf('/'));
            var key = s3Path.Substring(s3Path.IndexOf('/'));
            Assert.AreEqual("tgonzalez-image-archive", bucket);
            Assert.AreEqual("/national-gallery-of-art/image-80117.jpg", key);
        }

        /// <remarks>
        /// This image is over 12MB.
        /// There is no file size limit when the image is in s3 (or it's increased).
        /// No it's 15mb, but then how did I do the super high resolution images on the national gallery of art?
        /// I don't know, but they are 50mb plus. That's why I worked so hard to get them.
        /// </remarks>
        
        [Test]
        public void Test_Image_Analysis_From_Over_5MB_File()
        {
            //var sampleWorkId = 74404;
            //var sampleWorkId = 159426;
            //var sampleWorkId = 101307;
            var sampleWorkId = 40832; // This is a big fucking problem. This image has nudity of a minor so it's what I want to identify most, because it's the most shocking
            // and would need to be viewed in the appropriate time and place.
            // It's not something you would want displayed at your work.
            // But it's not obscene and his serious art so it's not terrible that it's not caught, but the problem is if it were obscene
            // I have to know so I can remove it. To not be caught as nudity misses the issue completely.
            // Well this at a minimum shows a due diligence and serves as evidence that I wasn't aware of it should some obscene impressionist image slip through.
            // And yet if it did slip through due to impressionist artistic style then it would likely be considered serious art and that's a topic I hope I don't have to deal with
            // which is serious art depicting a minor that is obscene. Technically it's legal, but I don't want to deal with that issue and I hope the sources I've gone after
            // have the same level of professionalism and it's something that doesn't come up.
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
                                new AttributeValue {S = "http://www.the-athenaeum.org" }
                            }
                        }
                    },
                    {
                        "pageId",
                        new Condition
                        {
                            ComparisonOperator = "EQ",
                            AttributeValueList =
                                new List<AttributeValue> {new AttributeValue {S = sampleWorkId.ToString()}}
                        }
                    }
                },
                Limit = 1
            };
            var dbResponse = client.QueryAsync(dbRequest).Result;
            var result = dbResponse.Items.Single();
            var sampleWork = JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(result).ToJson());

            var labels = GetImageAnalysis(rekognitionClient, NationalGalleryOfArtIndexer.BUCKET, sampleWork.S3Path);

            //Console.WriteLine(JsonConvert.SerializeObject(labels));

            // The image is of a half-circle of monks using a tall device to drive poles over the river to make a bridge on a farm.
            //Assert.IsTrue(labels.LabelsAndConfidence.Any(x => x.StartsWith("Flower Arrangement: 5")));
        }

        public List<ClassificationLabel> GetImageAnalysis(IAmazonRekognition rekognitionClient, string bucket, string s3Path)
        {
            var request = new DetectModerationLabelsRequest
            {
                Image = new Image
                {
                    S3Object = new Amazon.Rekognition.Model.S3Object
                    {
                        Bucket = bucket,
                        Name = s3Path
                    }
                }
            };
            var response = rekognitionClient.DetectModerationLabelsAsync(request).Result;
            return response.ModerationLabels.Select(x =>
                new ClassificationLabel
                {
                    Confidence = x.Confidence,
                    Name = x.Name,
                    ParentName = x.ParentName
                }).ToList();
        }

        /*
        private ImageLabel GetModerationLabels(ClassificationModel image)
        {
            var request = new DetectModerationLabelsRequest
            {
                Image = new Image
                {
                    S3Object = new S3Object
                    {
                        Bucket = NationalGalleryOfArtIndexer.BUCKET,
                        Name = image.S3Path
                    }
                }
            };
            
            var response = rekognitionClient.DetectModerationLabels(request);

            var imageLabel = new ImageLabel
            {
                Source = image.Source,
                PageId = int.Parse(image.PageId),
                S3Path = image.S3Path,
                LabelsAndConfidence = response
                    .ModerationLabels
                    .Select(x => x.Name + ": " + x.Confidence).ToList(),
                NormalizedLabels = response.ModerationLabels.Select(x => x.Name.ToLower()).ToList()
            };

            return imageLabel;
        }
        */
    }
}
