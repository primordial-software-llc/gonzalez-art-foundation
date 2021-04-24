using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Amazon.DynamoDBv2;
using Amazon.S3.Model;
using IndexBackend;
using IndexBackend.Model;
using IndexBackend.NationalGalleryOfArt;
using IndexBackend.Sources.NationalGalleryOfArt;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Tests.DataAccessTests
{
    class ImageClassificationAccessTests
    {

        private readonly IAmazonDynamoDB client = GalleryAwsCredentialsFactory.ProductionDbClient;

        [Test]
        public void Get_By_Key()
        {

            var awsToolsClient = new DatabaseClient<ClassificationModel>(client);
            var image = awsToolsClient.Get(new ClassificationModel
            {
                Source = NationalGalleryOfArtIndexer.Source,
                PageId = 18392.ToString()
            });
            Assert.AreEqual(18392.ToString(), image.PageId);
            Assert.IsNotNull(image.S3Path);
            Assert.IsNotEmpty(image.S3Path);
        }

        [Test]
        public void Get_Image()
        {
            var key = "national-gallery-of-art/image-18392.jpg";
            GetObjectResponse s3Object = GalleryAwsCredentialsFactory.S3AcceleratedClient.GetObjectAsync("tgonzalez-image-archive", key).Result;
            var memoryStream = new MemoryStream();
            s3Object.ResponseStream.CopyTo(memoryStream);

            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(memoryStream.ToArray())
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/" + key.Split('.').Last());
        }

    }
}
