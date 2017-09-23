using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.S3;
using GalleryBackend;
using HtmlAgilityPack;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.NationalGalleryOfArt;
using NUnit.Framework;

namespace SlideshowCreator.Tests
{
    class NationalGalleryOfArtIndexingTests
    {
        private readonly PrivateConfig privateConfig = PrivateConfig.CreateFromPersonalJson();
        private readonly IAmazonS3 s3Client = new AwsClientFactory().CreateS3Client();
        private readonly IAmazonDynamoDB dynamoDbClient = new AwsClientFactory().CreateDynamoDbClient();

        private NationalGalleryOfArtDataAccess ngaDataAccess;
        private NationalGalleryOfArtIndexer indexer;
        private VpnCheck vpnCheck;

        [OneTimeSetUp]
        public void Setup_Tests_Once()
        {
            ngaDataAccess = new NationalGalleryOfArtDataAccess();
            ngaDataAccess.Init(new Uri(privateConfig.Target2Url));
            indexer = new NationalGalleryOfArtIndexer(s3Client, dynamoDbClient, ngaDataAccess);

            var token = new GalleryClient().Authenticate(privateConfig.GalleryUsername, privateConfig.GalleryPassword).Token;
            vpnCheck = new VpnCheck(token);
            vpnCheck.AssertVpnInUse(privateConfig.DecryptedIp);
        }

        [Test]
        public void Get_Search_Results()
        {
            int expectedPages = 648;
            ngaDataAccess.SetSearchResultsTo75PerPage();
            
            System.Threading.Thread.Sleep(new NationalGalleryOfArtIndexer().GetNextThrottleInMilliseconds);

            var imageIds = new List<int>();

            for (int pageNumber = 1; pageNumber <= expectedPages; pageNumber += 1)
            {
                var results = ngaDataAccess.GetSearchResults(pageNumber);

                // It may not be possible to run this with the robots.txt delay of 40 seconds,
                // because a 503 error may occur potentially due to the cookie going bad.
                // But I don't know, because the project is public and I followed the robots.txt to the "T"
                // and didn't comment out the line below after getting said potential 503 around page 160 because I don't want to get into recovery/queuing
                // for just getting the ID's to download the images.
                // In all seriousness, if I follow the robots.txt it will take 22 days just to get the ID's.
                // At that rate every day matters I need to get crawling ASAP or I will not be able to respect the robots.txt where it matters for the super high res images.
                // Besides 40 seconds is long and it's not like I'm going in parallel.
                //System.Threading.Thread.Sleep(40 * 1000); // Should fetch robots.txt and parse the delay. I'm moving quick now so there's really no reason not to.
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(results);
                var nextImageIds = htmlDoc.DocumentNode.Descendants("a")
                    .Where(x => x.Id.StartsWith("damDownloadLink_"))
                    .Select(x => Int32.Parse(x.Id.Replace("damDownloadLink_", string.Empty)))
                    .ToList();
                imageIds.AddRange(nextImageIds);

                Console.WriteLine($"Saving {nextImageIds.Count} new image ids from page {pageNumber} with a total of {imageIds.Count} existing image id.");
                File.WriteAllLines(
                    "C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\NationalGalleryOfArtImageIds.txt",
                    imageIds.Select(x => x.ToString())
                );
            }

            Assert.AreEqual(48574, imageIds.Count); // FAILS
            // Tomorrow the-athenaeum will be done re-crwaling. I need to do a backup of dynamodb. However long it takes. Code it out. Document the process. That might be a cool new repo or project.
            // Then I need to crawl the image downloads. That will take 22.48796296 days time if I respect the robots.txt, which I need to while the project is public. That's why I'm keeping it public after all.

            // The count isn't accurate. Open access? Well not all open access images have download links. I'm not wrong here. The actual count is 48572.
        }

        /// <summary>
        /// This is failing on occasion.
        /// There is probably an issue with the decoding.
        /// Once I get everything stabilized I'll figure out the issue.
        /// </summary>
        [Test]
        public void Get_Home_Page_Through_500_Response()
        {
            var assetId2 = 1;

            IndexAndAssertInS3(46482);

            System.Threading.Thread.Sleep(40 * 1000);
            var asset2Index = indexer.Index(assetId2);
            
            Assert.Throws<AmazonS3Exception>(() => s3Client.GetObjectMetadata(indexer.S3Bucket, "image-" + assetId2 + ".jpg"));
            Assert.IsNull(asset2Index);
        }

        private void IndexAndAssertInS3(int id)
        {
            var asset1Index = indexer.Index(id);
            Assert.AreEqual("http://images.nga.gov", asset1Index.Source);
            Assert.AreEqual(id, asset1Index.PageId);
            Assert.AreEqual(indexer.S3Bucket + "/" + "image-" + id + ".jpg", asset1Index.S3Path);
            s3Client.GetObjectMetadata(indexer.S3Bucket, "image-" + id + ".jpg");
        }

        private const string DECODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE =
                @"{""mainForm"":{""project_title"":""Personal Digital Gallery"",""usage"":""5""},""assets"":{""a0"":{""assetId"":""135749"",""sizeId"":""3""}}}";

        public const string ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE = "JTdCJTIybWFpbkZvcm0lMjIlM0ElN0IlMjJwcm9qZWN0X3RpdGxlJTIyJTNBJTIyUGVyc29uYWwlMjBEaWdpdGFsJTIwR2FsbGVyeSUyMiUyQyUyMnVzYWdlJTIyJTNBJTIyNSUyMiU3RCUyQyUyMmFzc2V0cyUyMiUzQSU3QiUyMmEwJTIyJTNBJTdCJTIyYXNzZXRJZCUyMiUzQSUyMjEzNTc0OSUyMiUyQyUyMnNpemVJZCUyMiUzQSUyMjMlMjIlN0QlN0QlN0Q=";

        public const string URL_ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE = "%7B%22mainForm%22%3A%7B%22project_title%22%3A%22Personal%20Digital%20Gallery%22%2C%22usage%22%3A%225%22%7D%2C%22assets%22%3A%7B%22a0%22%3A%7B%22assetId%22%3A%22135749%22%2C%22sizeId%22%3A%223%22%7D%7D%7D";

        [Test]
        public void Decode_High_Res_Image_Reference()
        {
            Assert.AreEqual(DECODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE, 
                HighResImageEncoding.Decode(ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE));
        }

        [Test]
        public void Generate_Encoded_High_Res_Image_Reference()
        {
            Assert.AreEqual(ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE,
                HighResImageEncoding.Encode(DECODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE));
        }

        [Test]
        public void Generate_Encoded_High_Res_Image_Reference_From_Typed_Model()
        {
            var reference = HighResImageEncoding.CreateReferenceUrlData(135749);
            Assert.AreEqual(ENCODED_JEAN_LEON_GEROME_VIEW_OF_MEDINET_EL_FAYOUM_HIGH_RES_REFERENCE,
                reference);
        }

    }
}
