using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend.NationalGalleryOfArt;

namespace IndexBackend.Indexing
{
    public class NationalGalleryOfArtIndexer : IIndex
    {
        public static readonly string BUCKET = "tgonzalez-image-archive";
        public static readonly string S3_Path = "national-gallery-of-art";
        public string S3Bucket => BUCKET + "/" + S3_Path;
        public string Source => "http://images.nga.gov";
        public string IdFileQueuePath => "C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\NationalGalleryOfArtImageIds.txt";
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

        public ClassificationModel Index(int id)
        {
            var zipFile = NgaDataAccess.GetHighResImageZipFile(id);
            ClassificationModel classification = null;

            if (zipFile != null)
            {
                var key = SendToS3(zipFile, id);

                classification = new ClassificationModel
                {
                    Source = Source,
                    PageId = id,
                    S3Path = S3Bucket + "/" + key
                };

                var dynamoDbClassification = Conversion<ClassificationModel>.ConvertToDynamoDb(classification);
                DynamoDbClient.PutItem(new ClassificationModel().GetTable(), dynamoDbClassification);
            }

            return classification;
        }

        public void RefreshConnection()
        {
            NgaDataAccess.Init();
        }

        public string SendToS3(byte[] zipFile, int id)
        {
            string key = "image-" + id;
            using (MemoryStream zipFileStream = new MemoryStream(zipFile))
            using (ZipArchive archive = new ZipArchive(zipFileStream))
            {
                ZipArchiveEntry imgArchive = archive.Entries
                    .Single(x => ImageExtensions.Any(
                        imgExt => x.FullName.EndsWith(imgExt, StringComparison.OrdinalIgnoreCase)));
                key += "." + imgArchive.FullName.Split('.').Last();
                using (Stream imgStream = imgArchive.Open())
                {
                    PutObjectRequest request = new PutObjectRequest
                    {
                        BucketName = S3Bucket,
                        Key = key,
                        InputStream = imgStream
                    };
                    S3Client.PutObject(request);
                }
            }

            return key;
        }

        private List<string> ImageExtensions => new List<string> {".jpg",".tif"};

    }
}
