using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.S3.Model;
using IndexBackend.NationalGalleryOfArt;

namespace IndexBackend.Indexing
{
    public class NationalGalleryOfArtIndexer : IIndex
    {
        public string S3Bucket => "tgonzalez-image-archive/national-gallery-of-art";
        public string Source => "http://images.nga.gov";
        protected virtual IAmazonS3 S3Client { get; }
        protected virtual IAmazonDynamoDB DynamoDbClient { get; }
        protected virtual NationalGalleryOfArtDataAccess NgaDataAccess { get; set; }

        public NationalGalleryOfArtIndexer(IAmazonS3 s3Client, IAmazonDynamoDB dynamoDbClient)
        {
            S3Client = s3Client;
            DynamoDbClient = dynamoDbClient;
        }

        public void Init(string url)
        {
            var uri = new Uri(url);
            NgaDataAccess = new NationalGalleryOfArtDataAccess();
            NgaDataAccess.Init(uri);
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

                var classificationConversion = new ClassificationConversion();
                var dynamoDbClassification = classificationConversion.ConvertToDynamoDb(classification);
                DynamoDbClient.PutItem(ImageClassificationAccess.IMAGE_CLASSIFICATION_V2, dynamoDbClassification);
            }

            return classification;
        }


        public string SendToS3(byte[] zipFile, int id)
        {
            var imgExt = ".jpg";
            string key = "image-" + id + imgExt;
            using (MemoryStream zipFileStream = new MemoryStream(zipFile))
            using (ZipArchive archive = new ZipArchive(zipFileStream))
            {
                ZipArchiveEntry imgArchive = archive.Entries
                    .Single(x => x.FullName.EndsWith(imgExt));
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
    }
}
