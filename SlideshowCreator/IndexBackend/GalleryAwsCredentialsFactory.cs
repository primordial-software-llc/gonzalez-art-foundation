using System;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Rekognition;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.SQS;

namespace IndexBackend
{
    /// <summary>
    /// Credentials (nor any other secrets) must never be placed in source code.
    /// All source code must be public for maximum durability.
    /// Privacy shouldn't be assumed to exist in the cloud.
    /// </summary>
    public class GalleryAwsCredentialsFactory
    {
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

        public static IAmazonSQS SqsClient = new AmazonSQSClient(
            CreateCredentials(),
            new AmazonSQSConfig {RegionEndpoint = RegionEndpoint.USEast1});

        public static IAmazonS3 S3AcceleratedClient => new AmazonS3Client(
            CreateCredentials(),
            new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1,
                UseAccelerateEndpoint = true
            });

        public static IAmazonDynamoDB ProductionDbClient => new AmazonDynamoDBClient(
            CreateCredentials(),
            RegionEndpoint.USEast1);

        public static AmazonRekognitionClient RekognitionClientClient => new AmazonRekognitionClient(
            CreateCredentials(),
            RegionEndpoint.USEast1);
    }
}
