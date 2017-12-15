using System;
using System.Web;
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
        /// Get credentials from an EC2 instance's profile or from the Visual Studio AWS toolkit C:\Users\peon\AppData\Local.
        /// Credentials use the EC2 instance profile when domain is tgonzalez.net.
        /// </summary>
        /// <remarks>
        /// InstanceProfileAWSCredentials makes an API call for available roles, which is extremely slow to timeout causing delays on a test website which isn't an EC2 instance.
        /// </remarks>
        public static AWSCredentials GetCredentialsForWebsite()
        {
            if (HttpContext.Current != null &&
                (HttpContext.Current.Request.Url.Host.Equals("tgonzalez.net", StringComparison.OrdinalIgnoreCase) ||
                HttpContext.Current.Request.Url.Host.EndsWith("us-east-1.elasticbeanstalk.com", StringComparison.OrdinalIgnoreCase)))
            {
                return new InstanceProfileAWSCredentials();
            }
            else
            {
                return CreateCredentialsFromDefaultProfile();
            }
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
