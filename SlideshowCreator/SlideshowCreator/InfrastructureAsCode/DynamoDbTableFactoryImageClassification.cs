using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using IndexBackend.Model;

namespace SlideshowCreator.InfrastructureAsCode
{
    class DynamoDbTableFactoryImageClassification
    {
        private DynamoDbTableFactory TableFactory { get; }
        private IAmazonDynamoDB Client { get; }

        public DynamoDbTableFactoryImageClassification(IAmazonDynamoDB client)
        {
            TableFactory = new DynamoDbTableFactory(client);
            Client = client;
        }

        public static CreateTableRequest GetTableDefinition()
        {
            var request = new CreateTableRequest
            {
                TableName = new ClassificationModel().GetTable(),
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "source",
                        KeyType = "HASH"
                    },
                    new KeySchemaElement
                    {
                        AttributeName = "pageId",
                        KeyType = "RANGE"
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = "source",
                        AttributeType = "S"
                    },
                    new AttributeDefinition
                    {
                        AttributeName = "pageId",
                        AttributeType = "S"
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };

            return request;
        }

    }
}
