using System;
using System.Collections.Generic;
using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;
using SlideshowCreator.Classification;

namespace SlideshowCreator
{
    class DynamoDbTableFactory
    {
        public CreateTableRequest GetTableDefinition()
        {
            var request = new CreateTableRequest
            {
                TableName = "ImageClassification",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement
                    {
                        AttributeName = "pageId",
                        KeyType = "HASH"
                    },
                    new KeySchemaElement
                    {
                        AttributeName = "artist",
                        KeyType = "RANGE"
                    }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition
                    {
                        AttributeName = "pageId",
                        AttributeType = "N"
                    },
                    new AttributeDefinition
                    {
                        AttributeName = "artist",
                        AttributeType = "S"
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

        public void AA_Drop_Then_Create_Table(CreateTableRequest request, AmazonDynamoDBClient client)
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

        public Dictionary<string, List<WriteRequest>> GetBatchWriteRequest(List<ClassificationModel> classifications)
        {
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var batchWrite = new Dictionary<string, List<WriteRequest>> { [request.TableName] = new List<WriteRequest>() };

            foreach (var classification in classifications)
            {
                var dyamoDbModel = new ClassificationConversion().ConvertToDynamoDb(classification);
                var putRequest = new PutRequest(dyamoDbModel);
                var writeRequest = new WriteRequest(putRequest);
                batchWrite[request.TableName].Add(writeRequest);
            }

            return batchWrite;
        }
    }
}
