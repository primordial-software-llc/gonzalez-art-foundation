using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AwsTools;

namespace SlideshowCreator.InfrastructureAsCode
{
    class DynamoDbTableFactoryImageLabel
    {
        private DynamoDbTableFactory TableFactory { get; }

        public DynamoDbTableFactoryImageLabel(AmazonDynamoDBClient client)
        {
            TableFactory = new DynamoDbTableFactory(client);
        }

        public static CreateTableRequest GetTableDefinition<T>() where T : IModel, new()
        {
            var request = new CreateTableRequest
            {
                TableName = new T().GetTable(),
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

        public void CreateTable<T>() where T : IModel, new()
        {
            var request = GetTableDefinition<T>();
            TableFactory.CreateTable(request);
        }

    }
}
