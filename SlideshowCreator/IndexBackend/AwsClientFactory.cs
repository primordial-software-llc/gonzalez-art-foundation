using Amazon;
using Amazon.DynamoDBv2;
using Amazon.S3;

namespace IndexBackend
{
    public class AwsClientFactory
    {
        public AmazonDynamoDBClient CreateDynamoDbClient()
        {
            return new AmazonDynamoDBClient(
                GalleryAwsCredentialsFactory.CreateCredentialsFromDefaultProfile(),
                RegionEndpoint.USEast1);
        }

        public AmazonS3Client CreateS3Client()
        {
            return new AmazonS3Client(
                GalleryAwsCredentialsFactory.CreateCredentialsFromDefaultProfile(),
                RegionEndpoint.USEast1);
        }
    }
}
