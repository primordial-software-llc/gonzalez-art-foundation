using System;
using System.Collections.Generic;
using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using IndexBackend;
using NUnit.Framework;

namespace SlideshowCreator.InfrastructureAsCode
{
    class DynamoDbTableFactory
    {
        public CreateTableRequest GetTableDefinition()
        {
            var request = new CreateTableRequest
            {
                TableName = ImageClassificationAccess.IMAGE_CLASSIFICATION_V2,
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
                    WriteCapacityUnits = 1 // This actually needs to be 400 to do batches or go in parallel without getting unprocessed items (batches) or hitting capacity limits (parallel)
                }
            };

            return request;
        }

        public void CreateTableWithIndexes(AmazonDynamoDBClient client)
        {
            var tableFactory = new DynamoDbTableFactory();
            var request = tableFactory.GetTableDefinition();
            tableFactory.CreateTable(request, client);

            tableFactory.AddArtistNameGlobalSecondaryIndex(client, ImageClassificationAccess.IMAGE_CLASSIFICATION_V2);
        }

        private void CreateTable(CreateTableRequest request, AmazonDynamoDBClient client)
        {
            TableDescription tableDescription;
            var tableExists = TableExists(request.TableName);
            if (tableExists)
            {
                Console.WriteLine("Table found, deleting");
                DeleteTableResponse deleteResponse = client.DeleteTable(request.TableName);
                Assert.AreEqual(HttpStatusCode.OK, deleteResponse.HttpStatusCode);

                tableDescription = client.DescribeTable(request.TableName).Table;
                Assert.AreEqual(TableStatus.DELETING, tableDescription.TableStatus);
                do
                {

                    System.Threading.Thread.Sleep(200);
                    tableExists = TableExists(request.TableName);
                    Console.WriteLine("Table found after deleting, waiting.");
                } while (tableExists);
            }

            CreateTableResponse response = client.CreateTable(request);
            Assert.AreEqual(HttpStatusCode.OK, response.HttpStatusCode);

            tableDescription = client.DescribeTable(request.TableName).Table;
            Assert.AreEqual(TableStatus.CREATING, tableDescription.TableStatus);
            WaitForTableStatus(request.TableName, TableStatus.ACTIVE);
        }
        
        private void AddArtistNameGlobalSecondaryIndex(AmazonDynamoDBClient client, string tableName)
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

            client.UpdateTable(updateTableRequest);
        }

        private bool TableExists(string tableName)
        {
            bool tableExists;
            var client = new DynamoDbClientFactory().Create();
            try
            {
                client.DescribeTable(tableName);
                tableExists = true;
            }
            catch (ResourceNotFoundException)
            {
                tableExists = false;
            }
            return tableExists;
        }

        private void WaitForTableStatus(string tableName, TableStatus status)
        {
            TableDescription tableDescription;
            var client = new DynamoDbClientFactory().Create();
            do
            {
                System.Threading.Thread.Sleep(200);
                tableDescription = client.DescribeTable(tableName).Table;
                Console.WriteLine("Waiting for table status: " + status.Value);
            } while (tableDescription.TableStatus != status);
        }

    }
}
