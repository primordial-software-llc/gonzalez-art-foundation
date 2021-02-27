using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.S3;
using IndexBackend.Indexing;
using IndexBackend.Model;

namespace IndexBackend.NationalGalleryOfArt
{
    public class NationalGalleryOfArtIndexer : IIndex
    {
        public static readonly string BUCKET = "gonzalez-art-foundation";
        public string ImagePath => "collections/national-gallery-of-art";
        public string S3Bucket => BUCKET + "/" + ImagePath;
        public static string Source => "http://images.nga.gov";
        public int GetNextThrottleInMilliseconds => 0;

        protected IAmazonS3 S3Client { get; }
        protected IAmazonDynamoDB DynamoDbClient { get; }
        protected NationalGalleryOfArtDataAccess NgaDataAccess { get; set; }

        public NationalGalleryOfArtIndexer()
        {
            
        }

        public NationalGalleryOfArtIndexer(IAmazonS3 s3Client, IAmazonDynamoDB dynamoDbClient, NationalGalleryOfArtDataAccess ngaDataAccess)
        {
            S3Client = s3Client;
            DynamoDbClient = dynamoDbClient;
            NgaDataAccess = ngaDataAccess;
        }

        public void SetMetaData(ClassificationModel model)
        {
            Console.WriteLine("Getting metadata for page id " + model.PageId);
            var html = NgaDataAccess.GetAssetDetails(model.PageId);
            var details = AssetDetailsParser.ParseHtmlToNewModel(html);
            model.OriginalArtist = details.OriginalArtist;
            model.Artist = details.Artist;
            model.Name = details.Name;
            model.Date = details.Date;
            model.SourceLink = details.SourceLink;
        }

        public async Task<IndexResult> Index(string id)
        {
            var zipFile = await NgaDataAccess.GetHighResImageZipFile(id);
            if (zipFile == null)
            {
                return null;
            }
            byte[] imageBytes;
            using (MemoryStream zipFileStream = new MemoryStream(zipFile))
            using (ZipArchive archive = new ZipArchive(zipFileStream))
            {
                ZipArchiveEntry imgArchive = archive.Entries
                    .Single(x => ImageExtensions.Any(
                        imgExt => x.FullName.EndsWith(imgExt, StringComparison.OrdinalIgnoreCase)));
                using (var memoryStream = new MemoryStream())
                using (var imgStream = imgArchive.Open())
                {
                    imgStream.CopyTo(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }
                if (!imgArchive.FullName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                {
                    imageBytes = new IndexingHttpClient().ConvertToJpeg(imageBytes).Result;
                }
            }
            var classification = new ClassificationModel
            {
                Source = Source,
                PageId = id
            };
            SetMetaData(classification);
            return new IndexResult
            {
                Model = classification,
                ImageBytes = imageBytes
            };
        }

        private List<string> ImageExtensions => new List<string> {".jpg",".tif"};

    }
}
