
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using IndexBackend.NationalGalleryOfArt;

namespace IndexBackend.Indexing
{
    public class NationalGalleryOfArtIndexer : IIndex
    {
        public string S3Bucket => "tgonzalez-image-archive/national-gallery-of-art";
        public string Source => "http://images.nga.gov";
        protected  virtual IAmazonS3 S3Client { get; }

        public NationalGalleryOfArtIndexer(IAmazonS3 s3Client)
        {
            S3Client = s3Client;
        }

        public ClassificationModel Index(string url, int id)
        {
            var uri = new Uri(url);
            var ngaDataAccess = new NationalGalleryOfArtDataAccess();
            ngaDataAccess.Init(uri);

            var zipFile = ngaDataAccess.GetHighResImageZipFile(id);

            if (zipFile != null)
            {
                SendToS3(zipFile, id);
            }

            return null;
        }


        public void SendToS3(byte[] zipFile, int id)
        {
            using (MemoryStream zipFileStream = new MemoryStream(zipFile))
            using (ZipArchive archive = new ZipArchive(zipFileStream))
            {
                var imgExt = ".jpg";
                ZipArchiveEntry imgArchive = archive.Entries
                    .Single(x => x.FullName.EndsWith(imgExt));
                using (Stream imgStream = imgArchive.Open())
                {
                    PutObjectRequest request = new PutObjectRequest
                    {
                        BucketName = S3Bucket,
                        Key = "image-" + id + imgExt,
                        InputStream = imgStream
                    };
                    S3Client.PutObject(request);
                }
            }
        }
    }
}
