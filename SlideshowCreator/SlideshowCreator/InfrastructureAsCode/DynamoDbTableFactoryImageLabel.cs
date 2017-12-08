using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GalleryBackend;

namespace SlideshowCreator.InfrastructureAsCode
{
    class DynamoDbTableFactoryImageLabel
    {
        private DynamoDbTableFactory TableFactory { get; }
        private AmazonDynamoDBClient Client { get; }

        public DynamoDbTableFactoryImageLabel(AmazonDynamoDBClient client)
        {
            TableFactory = new DynamoDbTableFactory(client);
            Client = client;
        }

        public static CreateTableRequest GetTableDefinition()
        {
            var request = new CreateTableRequest
            {
                TableName = ImageClassification.TABLE_IMAGE_LABEL,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "s3Path", KeyType = "HASH" }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "s3Path", AttributeType = "S" }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };

            return request;
        }

        public void CreateTable()
        {
            var request = GetTableDefinition();
            TableFactory.CreateTable(request);
        }

    }
}
