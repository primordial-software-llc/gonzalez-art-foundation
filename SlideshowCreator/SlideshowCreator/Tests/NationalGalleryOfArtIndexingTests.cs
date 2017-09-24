using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            ngaDataAccess = new NationalGalleryOfArtDataAccess();
            ngaDataAccess.Init(new Uri(privateConfig.Target2Url));
            indexer = new NationalGalleryOfArtIndexer(s3Client, dynamoDbClient, ngaDataAccess);

            var galleryClient = new GalleryClient(privateConfig.GalleryUsername, privateConfig.GalleryPassword);
            vpnCheck = new VpnCheck(galleryClient);
            vpnCheck.AssertVpnInUse(privateConfig.DecryptedIp);
        }

        //[Test] This really is strictly production code. I have inder's which use id's, but the crawlers I'm working on that process to know what type of "stuff" needs to be indexed. So far crawling is stupid simple and indexing the "stuff" is the meat of the problem.
        public void Get_Search_Results()
        {
            int expectedPages = 648;
            ngaDataAccess.SetSearchResultsTo75PerPage();
            
            System.Threading.Thread.Sleep(new NationalGalleryOfArtIndexer().GetNextThrottleInMilliseconds);

            var imageIds = new List<int>();

            for (int pageNumber = 1; pageNumber <= expectedPages; pageNumber += 1)
            {
                var results = ngaDataAccess.GetSearchResults(pageNumber);

                // Might have problems here, because the cloudflare clearance token may go bad before this non-recoverable process completes.
                // Might have to take out the throttling or rather adjust.
                System.Threading.Thread.Sleep(new NationalGalleryOfArtIndexer().GetNextThrottleInMilliseconds);
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

            // Assert.AreEqual(48574, imageIds.Count); //  The actual count is 48572, some images don't have links even though the filter in use was "Open Access Available" specifically meaning there was a download link. Gosh must I test everything in this granular detail to just see basic things?
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

        [Test]
        public void ParallelIndexing()
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = -1
            };

            var validIdsIterable = ValidIds.ToList();

            Parallel.ForEach(validIdsIterable, parallelOptions, id =>
            {
                Console.WriteLine("Classifying " + id);
                indexer.Index(id);
                ValidIds.Remove(id);
                Console.WriteLine("Throttling.");
                Thread.Sleep(indexer.GetNextThrottleInMilliseconds);
            });

            Assert.AreEqual(0, ValidIds.Count); // Needs to update with removals.
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

        private List<int> ValidIds = new List<int>
        {
            33000,
            87551,
            24822,
            106015,
            100912,
            64536,
            106620,
            92478,
            89496,
            89598,
            59078,
            89057,
            99440,
            43938,
            21301,
            94863,
            114557,
            81396
        };

    }
}
