using System;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;

namespace IndexBackend
{
    /// <summary>
    /// Credentials (nor any other secrets) must never be placed in source code.
    /// All source code must be public for maximum durability.
    /// Privacy shouldn't be assumed to exist in the cloud.
    /// </summary>
    public class GalleryAwsCredentialsFactory
    {
        /// <summary>
        /// 1. Get credentials from an EC2 instance's profile.
        /// 2. Get credentials from the Visual Studio AWS toolkit C:\Users\peon\AppData\Local
        /// </summary>
        /// <remarks>
        /// Website may be run from an EC2 instance or locally so two locations must be searched.
        /// </remarks>
        public static AWSCredentials GetCredentialsForWebsite()
        {
            AWSCredentials credentials;
            try
            {
                credentials = new InstanceProfileAWSCredentials();
            }
            catch (AmazonServiceException credentialsException)
            {
                if (credentialsException.Message.Equals("Unable to reach credentials server"))
                {
                    credentials = CreateCredentialsFromDefaultProfile();
                }
                else throw;
            }
            return credentials;
        }

        public static IAmazonS3 S3Client => new AmazonS3Client(
            GetCredentialsForWebsite(),
            RegionEndpoint.USEast1);

        public static AWSCredentials CreateCredentialsFromDefaultProfile()
        {
            var chain = new CredentialProfileStoreChain();
            var profile = "default";
            if (!chain.TryGetAWSCredentials("default", out AWSCredentials awsCredentials))
            {
                throw new Exception($"c not found for \"{profile}\" profile.");
            }
            return awsCredentials;
        }
    }
}
