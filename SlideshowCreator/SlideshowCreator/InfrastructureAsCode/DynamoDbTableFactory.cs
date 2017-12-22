
using System;
using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;

namespace SlideshowCreator.InfrastructureAsCode
{
    class DynamoDbTableFactory
    {
        private IAmazonDynamoDB Client { get; }

        public DynamoDbTableFactory(IAmazonDynamoDB client)
        {
            Client = client;
        }

        public void CreateTable(CreateTableRequest request)
        {
            TableDescription tableDescription;
            var tableExists = TableExists(request.TableName);
            if (tableExists)
            {
                Console.WriteLine("Table found, deleting");
                DeleteTableResponse deleteResponse = Client.DeleteTable(request.TableName);
                Assert.AreEqual(HttpStatusCode.OK, deleteResponse.HttpStatusCode);

                tableDescription = Client.DescribeTable(request.TableName).Table;
                Assert.AreEqual(TableStatus.DELETING, tableDescription.TableStatus);
                do
                {

                    System.Threading.Thread.Sleep(200);
                    tableExists = TableExists(request.TableName);
                    Console.WriteLine("Table found after deleting, waiting.");
                } while (tableExists);
            }

            CreateTableResponse response = Client.CreateTable(request);
            Assert.AreEqual(HttpStatusCode.OK, response.HttpStatusCode);

            tableDescription = Client.DescribeTable(request.TableName).Table;
            Assert.AreEqual(TableStatus.CREATING, tableDescription.TableStatus);
            WaitForTableStatus(request.TableName, TableStatus.ACTIVE);
        }

        private bool TableExists(string tableName)
        {
            bool tableExists;
            try
            {
                Client.DescribeTable(tableName);
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
            do
            {
                System.Threading.Thread.Sleep(200);
                tableDescription = Client.DescribeTable(tableName).Table;
                Console.WriteLine("Waiting for table status: " + status.Value);
            } while (tableDescription.TableStatus != status);
        }

    }
}
