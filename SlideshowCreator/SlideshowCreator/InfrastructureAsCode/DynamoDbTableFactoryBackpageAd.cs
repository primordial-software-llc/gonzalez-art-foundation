using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using IndexBackend.DataAccess;

namespace SlideshowCreator.InfrastructureAsCode
{
    class DynamoDbTableFactoryBackpageAd
    {
        private DynamoDbTableFactory TableFactory { get; }

        public DynamoDbTableFactoryBackpageAd(AmazonDynamoDBClient client)
        {
            TableFactory = new DynamoDbTableFactory(client);
        }

        public void CreateTableWithIndexes(AmazonDynamoDBClient client, string tableName)
        {
            var request = GetTableDefinition(tableName);
            TableFactory.CreateTable(request);

            AddAgeGlobalSecondaryIndex(client, tableName);
        }

        private CreateTableRequest GetTableDefinition(string tableName)
        {
            var request = new CreateTableRequest
            {
                TableName = tableName,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "source",
                        KeyType = "HASH"
                    },
                    new KeySchemaElement
                    {
                        AttributeName = "url",
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
                        AttributeName = "url",
                        AttributeType = "S"
                    }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 5,
                    WriteCapacityUnits = 5
                }
            };

            return request;
        }
        
        private void AddAgeGlobalSecondaryIndex(AmazonDynamoDBClient client, string tableName)
        {
            var indexRequest = new GlobalSecondaryIndexUpdate
            {
                Create = new CreateGlobalSecondaryIndexAction
                {
                    IndexName = BackpageAdAccess.INDEX_AGE,
                    ProvisionedThroughput = new ProvisionedThroughput(5, 5),
                    Projection = new Projection {ProjectionType = ProjectionType.ALL},
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement {AttributeName = "age", KeyType = "HASH"},
                    }
                }
            };

            var updateTableRequest = new UpdateTableRequest {TableName = tableName};
            updateTableRequest.GlobalSecondaryIndexUpdates.Add(indexRequest);
            updateTableRequest.AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "age",
                    AttributeType = "N"
                }
            };

            client.UpdateTable(updateTableRequest);
        }

    }
}
