using System;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.NationalGalleryOfArt;
using SlideshowCreator;

namespace SlideshowIndexer
{
    class Program
    {

        static void Main(string[] args)
        {
            IIndex indexer = GetIndexer(IndexType.NationalGalleryOfArt);
            var fileIdQueueIndexer = new FileIdQueueIndexer();

            try
            {
                fileIdQueueIndexer.Index(indexer, "file path goes here if you use this again instead of sqs");
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
                var ngaDataAccess = new NationalGalleryOfArtDataAccess(PublicConfig.NationalGalleryOfArtUri);
                var indexer = new NationalGalleryOfArtIndexer(GalleryAwsCredentialsFactory.S3AcceleratedClient, GalleryAwsCredentialsFactory.ProductionDbClient, ngaDataAccess);
                return indexer;
            }
            else if (indexType == IndexType.TheAthenaeum)
            {
                return new TheAthenaeumIndexer(
                    "PrivateConfig.PageNotFoundIndicatorText", //         private static readonly PrivateConfig PrivateConfig = PrivateConfig.CreateFromPersonalJson();
                    GalleryAwsCredentialsFactory.ProductionDbClient,
                    PublicConfig.TheAthenaeumArt);
            }
            else
            {
                return null;
            }
        }

    }
}
