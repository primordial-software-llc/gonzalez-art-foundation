using System;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace SlideshowCreator
{
    class DynamoDbClientFactory
    {
        /// <summary>
        /// The credentials are retrieved from an encrypted file here:
        /// C:\Users\peon\AppData\Local\AWSToolkit\RegisteredAccounts
        /// Credentials are configured by the Visual Studio AWS toolkit used for publishing.
        /// </summary>
        public AmazonDynamoDBClient Create()
        {
            var chain = new CredentialProfileStoreChain();
            var profile = "default";
            if (!chain.TryGetAWSCredentials("default", out AWSCredentials awsCredentials))
            {
                throw new Exception($"Credentials not found for \"{profile}\" profile.");
            }
            var client = new AmazonDynamoDBClient(awsCredentials, RegionEndpoint.USEast1);
            return client;
        }
    }
}
