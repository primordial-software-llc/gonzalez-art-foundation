using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using GalleryBackend.Model;
using IndexBackend.DataAccess;

namespace SlideshowCreator.InfrastructureAsCode
{
    class DynamoDbTableFactoryImageClassification
    {
        private DynamoDbTableFactory TableFactory { get; }
        private AmazonDynamoDBClient Client { get; }

        public DynamoDbTableFactoryImageClassification(AmazonDynamoDBClient client)
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
                        AttributeType = "N"
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

        public void CreateTableWithIndexes()
        {
            var request = GetTableDefinition();
            TableFactory.CreateTable(request);
            AddArtistNameGlobalSecondaryIndex(new ClassificationModel().GetTable());
        }
        
        private void AddArtistNameGlobalSecondaryIndex(string tableName)
        {
            var artistNameIndexRequest = new GlobalSecondaryIndexUpdate
            {
                Create = new CreateGlobalSecondaryIndexAction
                {
                    IndexName = ImageClassificationAccess.ARTIST_NAME_INDEX,
                    ProvisionedThroughput = new ProvisionedThroughput(25, 5),
                    Projection = new Projection {ProjectionType = ProjectionType.ALL},
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement {AttributeName = "artist", KeyType = "HASH"},
                        new KeySchemaElement {AttributeName = "name", KeyType = "RANGE"}
                    }
                }
            };

            var updateTableRequest = new UpdateTableRequest {TableName = tableName};
            updateTableRequest.GlobalSecondaryIndexUpdates.Add(artistNameIndexRequest);
            updateTableRequest.AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "artist",
                    AttributeType = "S"
                },
                new AttributeDefinition
                {
                    AttributeName = "name",
                    AttributeType = "S"
                }
            };

            Client.UpdateTable(updateTableRequest);
        }

    }
}
