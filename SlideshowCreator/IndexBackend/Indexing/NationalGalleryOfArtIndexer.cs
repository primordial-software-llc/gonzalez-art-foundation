using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.S3.Model;
using AwsTools;
using GalleryBackend.Model;
using IndexBackend.NationalGalleryOfArt;

namespace IndexBackend.Indexing
{
    public class NationalGalleryOfArtIndexer : IIndex
    {
        public static readonly string BUCKET = "gonzalez-art-foundation";
        public static readonly string S3_Path = "collections/national-gallery-of-art";
        public string S3Bucket => BUCKET + "/" + S3_Path;
        public string Source => "http://images.nga.gov";
        public string IdFileQueuePath => "C:\\Users\\peon\\Desktop\\projects\\gonzalez-art-foundation-api\\SlideshowCreator\\NationalGalleryOfArtImageIds.txt";
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

        public void SetMetaData(ClassificationModelNew model)
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

        public ClassificationModelNew Index(int id)
        {
            var zipFile = NgaDataAccess.GetHighResImageZipFile(id);
            ClassificationModelNew classification = null;

            if (zipFile != null)
            {
                var key = SendToS3(zipFile, id);

                classification = new ClassificationModelNew
                {
                    Source = Source,
                    PageId = id,
                    S3Path = key
                };
                SetMetaData(classification);

                var dynamoDbClassification = Conversion<ClassificationModelNew>.ConvertToDynamoDb(classification);
                DynamoDbClient.PutItem(new ClassificationModelNew().GetTable(), dynamoDbClassification);
            }

            return classification;
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
                        InputStream = imgStream,
                        
                    };
                    S3Client.PutObject(request);
                }
            }

            return key;
        }

        private List<string> ImageExtensions => new List<string> {".jpg",".tif"};

    }
}
