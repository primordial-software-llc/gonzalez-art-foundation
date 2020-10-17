using System;
using System.Diagnostics;
using Amazon.DynamoDBv2.Model;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.NationalGalleryOfArt;

namespace SlideshowIndexer
{
    class Program
    {

        static void Main(string[] args)
        {
            /*
            var galleryClient = new GalleryClient(
                "tgonzalez.net",
                PrivateConfig.GalleryUsername,
                PrivateConfig.GalleryPassword);
            var vpnCheck = new VpnCheck(galleryClient);
            var vpnInUse = vpnCheck.IsVpnInUse(PrivateConfig.DecryptedIp);

            if (!string.IsNullOrWhiteSpace(vpnInUse))
            {
                Console.WriteLine(vpnInUse);
                return;
            }
            Console.WriteLine("VPN is in use with IP: " + vpnInUse);
            */
            IIndex indexer = GetIndexer(IndexType.NationalGalleryOfArt);
            var fileIdQueueIndexer = new FileIdQueueIndexer();

            try
            {
                fileIdQueueIndexer.Index(indexer);
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
                Debugger.Launch();
                var ngaDataAccess = new NationalGalleryOfArtDataAccess(PublicConfig.NationalGalleryOfArtUri);
                ngaDataAccess.Init();
                var indexer = new NationalGalleryOfArtIndexer(GalleryAwsCredentialsFactory.S3AcceleratedClient, GalleryAwsCredentialsFactory.DbClient, ngaDataAccess);
                return indexer;
            }
            else if (indexType == IndexType.TheAthenaeum)
            {
                return new TheAthenaeumIndexer(
                    "PrivateConfig.PageNotFoundIndicatorText", //         private static readonly PrivateConfig PrivateConfig = PrivateConfig.CreateFromPersonalJson();
                    GalleryAwsCredentialsFactory.DbClient,
                    PublicConfig.TheAthenaeumArt);
            }
            else
            {
                return null;
            }
        }

    }
}
