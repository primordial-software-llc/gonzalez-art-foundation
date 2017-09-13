using Amazon;
using Amazon.DynamoDBv2;

namespace IndexBackend
{
    public class DynamoDbClientFactory
    {
        public AmazonDynamoDBClient Create()
        {
            return new AmazonDynamoDBClient(
                GalleryAwsCredentialsFactory.CreateCredentialsFromDefaultProfile(),
                RegionEndpoint.USEast1);
        }
    }
}
