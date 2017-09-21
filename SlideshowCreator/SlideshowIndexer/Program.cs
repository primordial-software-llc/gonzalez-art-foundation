using System;
using Amazon.DynamoDBv2;
using Amazon.S3;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.NationalGalleryOfArt;

namespace SlideshowIndexer
{
    class Program
    {
        private static readonly PrivateConfig PrivateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");

        private static readonly IAmazonS3 S3Client = new AwsClientFactory().CreateS3Client();
        private static readonly IAmazonDynamoDB DynamoDbClient = new AwsClientFactory().CreateDynamoDbClient();

        static void Main(string[] args)
        {
            var vpnInUse = new VpnCheck().IsVpnInUse(PrivateConfig);

            if (!string.IsNullOrWhiteSpace(vpnInUse))
            {
                Console.WriteLine(vpnInUse);
                return;
            }

            IIndex indexer = GetIndexer(IndexType.NationalGalleryOfArt);
            var fileIdQueueIndexer = new FileIdQueueIndexer();

            Console.CancelKeyPress += (sender, eventArgs) => CleanForShutdown();

            try
            {
                using (PreventSleep preventSleep = new PreventSleep())
                {
                    preventSleep.DontAllowSleep();
                    fileIdQueueIndexer.Index(indexer);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        enum IndexType
        {
            NationalGalleryOfArt,
            TheAthenaeum
        }

        private static IIndex GetIndexer(IndexType indexType)
        {
            if (indexType == IndexType.NationalGalleryOfArt)
            {
                var ngaDataAccess = new NationalGalleryOfArtDataAccess();
                ngaDataAccess.Init(new Uri(PrivateConfig.Target2Url));
                var indexer = new NationalGalleryOfArtIndexer(S3Client, DynamoDbClient, ngaDataAccess);
                return indexer;
            }
            else if (indexType == IndexType.TheAthenaeum)
            {
                return new TheAthenaeumIndexer(
                    PrivateConfig.PageNotFoundIndicatorText,
                    DynamoDbClient,
                    PrivateConfig.TargetUrl);
            }
            else
            {
                return null;
            }
        }

        private static void CleanForShutdown()
        {
            Console.WriteLine("Cleaning up for user requested shutdown.");
            PreventSleep.AllowSleep();
        }

    }
}
