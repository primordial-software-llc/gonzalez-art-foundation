using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using SlideshowCreator.Models;

namespace SlideshowCreator
{
    class DataClassificationPersistenceExecutor
    {

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

        [TestCase]
        //[TestCase] Have to run this a few times if changes are made in order to test the test.
        //[TestCase]
        public void A_Drop_Then_Create_Table()
        {
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();

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

        List<Dictionary<string, AttributeValue>> classifications = new List<Dictionary<string, AttributeValue>>();

        [Test]
        public void B_Pull_Records_With_ID()
        {
            string[] files = Directory.GetFiles(DataDump.CLASSIFICATION_ARCHIVE);

            foreach (var fileName in files.Take(1000))
            {
                string rawPageId = fileName
                    .Replace(DataDump.CLASSIFICATION_ARCHIVE + "\\", String.Empty)
                    .Replace(DataDump.FILE_IDENTITY_TEMPLATE, string.Empty)
                    .Replace(".json", string.Empty);
                int pageId = int.Parse(rawPageId);

                var jsonClassifiction = File.ReadAllText(fileName);
                var classifiation = JsonConvert.DeserializeObject<Classification>(jsonClassifiction);
                classifiation.PageId = pageId;

                var kvp = new Dictionary<string, AttributeValue>
                {
                    {"artist", new AttributeValue {S = classifiation.Artist}},
                    {"name", new AttributeValue {S = classifiation.Name}},
                    {"date", new AttributeValue {S = classifiation.Date}},
                    {"imageId", new AttributeValue {N = classifiation.ImageId.ToString()}},
                    {"pageId", new AttributeValue {N = classifiation.PageId.ToString()}}
                };

                classifications.Add(kvp);
            }
        }

        List<List<Dictionary<string, AttributeValue>>> classificationBatches = new List<List<Dictionary<string, AttributeValue>>>();
        int batchSize = 25;

        [Test]
        public void C_Group_Into_Batches()
        {
            while (classifications.Any())
            {
                classificationBatches.Add(classifications.Take(batchSize).ToList());
                classifications = classifications.Skip(batchSize).ToList();
            }
        }
        
        private List<Dictionary<string, List<WriteRequest>>> AllUnprocessedItems = new List<Dictionary<string, List<WriteRequest>>>();
        [Test]
        public void D_Insert_Sample()
        { 
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();

            foreach (var batch in classificationBatches)
            {
                var batchWriteRequest =
                    new BatchWriteItemRequest(
                        new Dictionary<string, List<WriteRequest>> {[request.TableName] = new List<WriteRequest>()});

                foreach (var classification in batch)
                {
                    var putRequest = new PutRequest(classification);
                    var writeRequest = new WriteRequest(putRequest);
                    batchWriteRequest.RequestItems[request.TableName].Add(writeRequest);
                }

                File.WriteAllText("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\DynamoDbProgress.txt", JsonConvert.SerializeObject(batch));
                BatchWriteItemResponse batchWriteResponse = client.BatchWriteItem(batchWriteRequest);

                Dictionary<string, List<WriteRequest>> unprocessed = batchWriteResponse.UnprocessedItems;
                AllUnprocessedItems.Add(unprocessed);
            }
        }

        //[Test]
        public void E_Retry_Failed()
        {
            if (AllUnprocessedItems.Count == 0)
            {
                return;
            }
            var client = new DynamoDbClientFactory().Create();

            foreach (var unprocessedItem in AllUnprocessedItems)
            {
                File.WriteAllText("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\DynamoDbProgress.txt", JsonConvert.SerializeObject(unprocessedItem));
                BatchWriteItemResponse batchWriteResponse = client.BatchWriteItem(unprocessedItem);
                Assert.AreEqual(0, batchWriteResponse.UnprocessedItems.Count);
            }
        }

    }
}
