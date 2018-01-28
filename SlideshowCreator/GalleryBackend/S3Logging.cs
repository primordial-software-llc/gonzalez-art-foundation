using System;
using Amazon.S3;
using Amazon.S3.Model;
using AwsTools;
using Xunit.Abstractions;

namespace GalleryBackend
{
    public class S3Logging : ILogging, ITestOutputHelper
    {
        private readonly string bucket;
        private readonly string prefix;
        private readonly IAmazonS3 s3Client;
        
        /// <param name="prefix">For example: access-logs</param>
        /// <param name="s3Client"></param>
        public S3Logging(string prefix, IAmazonS3 s3Client)
            : this("tgonzalez-quick-logging", prefix, s3Client)
        {

        }

        public S3Logging(string bucket, string prefix, IAmazonS3 s3Client)
        {
            this.bucket = bucket;
            this.prefix = prefix;
            this.s3Client = s3Client;
        }

        /// <summary>
        /// Randomizing s3 file names is recommended by AWS to maximize read/writes when auto-scaling.
        /// </summary>
        /// <param name="message"></param>
        public void Log(string message)
        {
            var date = DateTime.UtcNow;
            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key =
                    $"{prefix}/{date:yyyy}/{date:MM}/{date:dd}/{date:HH}/{date:mm}/{Guid.NewGuid().ToString().Substring(0, 6)}-{date:ss}Z",
                ContentBody = message
            };
            s3Client.PutObject(request);
        }

        public void WriteLine(string message)
        {
            Log(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }
    }
}
