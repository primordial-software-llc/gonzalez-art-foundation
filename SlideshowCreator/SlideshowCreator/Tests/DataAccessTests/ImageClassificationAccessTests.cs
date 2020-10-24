using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Amazon.DynamoDBv2;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.DataAccess;
using IndexBackend.Indexing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class ImageClassificationAccessTests
    {

        private readonly IAmazonDynamoDB client = GalleryAwsCredentialsFactory.DbClient;


        [Test]
        public void Test_Find_By_Label()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.FindByLabel("Ancient Egypt", new NationalGalleryOfArtIndexer().Source);
            Assert.AreEqual(2871, results.Count);
            results = results
                .Where(
                    x => x.LabelsAndConfidence.Any(y =>
                        y.ToLower().StartsWith("ancient egypt: 99", StringComparison.OrdinalIgnoreCase))
                )
                .ToList();
            foreach (var result in results)
            {
                foreach (var resultLabel in result.LabelsAndConfidence)
                {
                    Console.WriteLine(resultLabel);
                }
                Console.WriteLine(result.S3Path);
            }
            Console.WriteLine(results.Count);
            Assert.GreaterOrEqual(results.Count, 2);
        }

        [Test]
        public void Test_Get_Labels()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.GetLabel(118814);
            Console.WriteLine(JsonConvert.SerializeObject(results));
            // [{"s3Path":"tgonzalez-image-archive/national-gallery-of-art/image-118814.jpg","source":"http://images.nga.gov","pageId":118814,"Labels":["Ancient Egypt: 55.88667","Art: 75.78365","Collage: 53.46183","Drawing: 67.61792","Fossil: 93.04782","Mosaic: 51.66335","Ornament: 75.78365","Outdoors: 57.51022","Paper: 59.10925","Poster: 53.46183","Sand: 57.51022","Sketch: 67.61792","Soil: 57.51022","Tapestry: 75.78365","Tile: 51.66335"],"normalizedLabels":["ancient egypt","art","collage","drawing","fossil","mosaic","ornament","outdoors","paper","poster","sand","sketch","soil","tapestry","tile"]}]
            Assert.IsTrue(results.LabelsAndConfidence.Any(x => x.StartsWith("Ancient Egypt", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(results.LabelsAndConfidence.Any(x => x.StartsWith("Outdoors")));
        }

        [Test]
        public void Test_Scan()
        {
            var dataAccess = new ImageClassificationAccess(client);
            var results = dataAccess.Scan(0, new TheAthenaeumIndexer().Source, 10);
            Assert.AreEqual(10, results.Count);
            Assert.AreEqual(33, results.First().PageId);
            Assert.AreEqual(42, results.Last().PageId);
            var results2 = dataAccess.Scan(results.Last().PageId, new TheAthenaeumIndexer().Source, 10);
            Assert.AreEqual(43, results2.First().PageId);
        }

        [Test]
        public void Get_By_Key()
        {

            var awsToolsClient = new DynamoDbClient<ClassificationModel>(client, new ConsoleLogging());
            var image = awsToolsClient.Get(new ClassificationModel
            {
                Source = new NationalGalleryOfArtIndexer().Source,
                PageId = 18392
            }).Result;
            Assert.AreEqual(18392, image.PageId);
            Assert.IsNotNull(image.S3Path);
            Assert.IsNotEmpty(image.S3Path);
        }

        [Test]
        public void Get_Image()
        {
            var key = "national-gallery-of-art/image-18392.jpg";
            GetObjectResponse s3Object = GalleryAwsCredentialsFactory.S3AcceleratedClient.GetObject("tgonzalez-image-archive", key);
            var memoryStream = new MemoryStream();
            s3Object.ResponseStream.CopyTo(memoryStream);

            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new ByteArrayContent(memoryStream.ToArray());
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/" + key.Split('.').Last());
        }

    }
}
