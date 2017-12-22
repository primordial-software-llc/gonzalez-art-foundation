using IndexBackend;
using SlideshowCreator.InfrastructureAsCode;

namespace SlideshowCreator.Scripts
{
    class DeployImageTable
    {
        //[Test] CAUTION: This script will delete the table and its data if it exists. Create a backup first!
        public void Deploy()
        {
            var client = GalleryAwsCredentialsFactory.DbClient;
            new DynamoDbTableFactoryImageClassification(client).CreateTableWithIndexes();
        }
    }
}
