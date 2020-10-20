using System;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using NUnit.Framework;

namespace SlideshowCreator.Tests
{
    class UploadToS3
    {

        public static RegionEndpoint HomeRegion => RegionEndpoint.USEast1;
        public static AWSCredentials CreateCredentials()
        {
            var chain = new CredentialProfileStoreChain();
            var profile = "gonzalez-art-foundation";
            if (!chain.TryGetAWSCredentials(profile, out AWSCredentials awsCredentials))
            {
                throw new Exception($"AWS credentials not found for \"{profile}\" profile.");
            }
            return awsCredentials;
        }

        [Test]
        public void SendToS3()
        {
            var files = Directory.GetFiles("G:\\Data\\ImageArchive")
                .Where(x => !string.Equals(x, "G:\\Data\\ImageArchive\\desktop.ini"))
                .ToList();

            foreach (var file in files)
            {
                var fileName = file.Split('\\').Last();
                Console.WriteLine(fileName);
                var s3AcceleratedClient = new AmazonS3Client(
                    CreateCredentials(),
                    new AmazonS3Config
                    {
                        RegionEndpoint = RegionEndpoint.USEast1,
                        UseAccelerateEndpoint = true
                    });

                using (FileStream fs = File.OpenRead(file))
                {
                    s3AcceleratedClient.PutObject(new PutObjectRequest
                    {
                        BucketName = "gonzalez-art-foundation/collections/the-athenaeum",
                        Key = fileName,
                        InputStream = fs
                    });
                }
            }

        }
    }
}
