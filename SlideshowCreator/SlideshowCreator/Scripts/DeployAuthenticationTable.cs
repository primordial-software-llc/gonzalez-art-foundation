using System;
using System.Collections.Generic;
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
        //[Test]
        public void Rebuild_Table()
        {
            var client = new AwsClientFactory().CreateDynamoDbClient();

            var request = new CreateTableRequest
            {
                TableName = new GalleryUser().GetTable(),
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
            });
        }

    }
}
