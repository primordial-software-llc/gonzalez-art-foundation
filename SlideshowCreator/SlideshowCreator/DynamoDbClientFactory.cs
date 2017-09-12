using Amazon;
using Amazon.DynamoDBv2;
using GalleryBackend.DataAccess;

namespace SlideshowCreator
{
    class DynamoDbClientFactory
    {
        public AmazonDynamoDBClient Create()
        {
            return new AmazonDynamoDBClient(
                GalleryAwsCredentialsFactory.CreateCredentialsFromDefaultProfile(),
                RegionEndpoint.USEast1);
        }

    }
}
