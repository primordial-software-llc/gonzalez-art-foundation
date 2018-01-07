using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using SlideshowCreator.InfrastructureAsCode;

namespace SlideshowCreator.Scripts
{
    class DeployAuthenticationTable
    {
        private string table = new GalleryUser().GetTable();

        //[Test]
        public void Rebuild_Table()
        {
            var client = GalleryAwsCredentialsFactory.DbClient;

            var request = new CreateTableRequest
            {
                TableName = table,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "id", KeyType = "HASH" }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "id", AttributeType = "S" }
                },
                ProvisionedThroughput = new ProvisionedThroughput
                {
                    ReadCapacityUnits = 1,
                    WriteCapacityUnits = 1
                }
            };

            var tableFactory = new DynamoDbTableFactory(client);
            tableFactory.CreateTable(request);
            var awsToolsClient = new DynamoDbClient<GalleryUser>(client, new ConsoleLogging());
            var username = "REDACTED";
            var password = "REDACTED";
            awsToolsClient.Insert(new GalleryUser
            {
                Id = Guid.NewGuid().ToString(),
                Hash = Authentication.Hash($"{username}:{password}")
            }).Wait();
        }

        //[Test]
        public void Create_User_Hash_Index()
        {   
            var userHashIndexRequest = new GlobalSecondaryIndexUpdate
            {
                Create = new CreateGlobalSecondaryIndexAction
                {
                    IndexName = GalleryUser.USER_HASH_INDEX,
                    ProvisionedThroughput = new ProvisionedThroughput(1,1),
                    Projection = new Projection {ProjectionType = ProjectionType.ALL},
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement {AttributeName = "hash", KeyType = "HASH"},
                    }
                }
            };

            var updateTableRequest = new UpdateTableRequest {TableName = table};
            updateTableRequest.GlobalSecondaryIndexUpdates.Add(userHashIndexRequest);
            updateTableRequest.AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition
                {
                    AttributeName = "hash",
                    AttributeType = "S"
                }
            };

            GalleryAwsCredentialsFactory.DbClient.UpdateTable(updateTableRequest);
        }

    }
}
