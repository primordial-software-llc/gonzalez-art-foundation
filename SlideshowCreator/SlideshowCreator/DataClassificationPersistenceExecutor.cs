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
        private int batchSize = 25;

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

        private Dictionary<string, AttributeValue> ConvertToNameValuePair(Classification classification)
        {
            var kvp = new Dictionary<string, AttributeValue>();
            kvp.Add("pageId", new AttributeValue { N = classification.PageId.ToString() });

            kvp.Add("artist", new AttributeValue { S = string.IsNullOrWhiteSpace(classification.Artist)
                ? DataClassifier.UNKNOWN_ARTIST
                : classification.Artist });

            if (classification.ImageId > 0)
            {
                kvp.Add("imageId", new AttributeValue { N = classification.ImageId.ToString() });
            }

            if (!string.IsNullOrWhiteSpace(classification.Name))
            {
                kvp.Add("name", new AttributeValue { S = classification.Name });
            }

            if (!string.IsNullOrWhiteSpace(classification.Date))
            {
                kvp.Add("date", new AttributeValue { S = classification.Date });
            }

            return kvp;
        }


        public Dictionary<string, List<WriteRequest>> GetBatchWriteRequest(List<Classification> classifications)
        {
            var request = new DynamoDbTableFactory().GetTableDefinition();
            
            var batchWrite = new Dictionary<string, List<WriteRequest>> { [request.TableName] = new List<WriteRequest>() };

            foreach (var classification in classifications)
            {
                var putRequest = new PutRequest(ConvertToNameValuePair(classification));
                var writeRequest = new WriteRequest(putRequest);
                batchWrite[request.TableName].Add(writeRequest);
            }

            return batchWrite;
        }

        /// <summary>
        /// Something is wrong with the de-serialization of the values into the write requests.
        /// </summary>
        /// <returns></returns>
        private Classification GetClassificationFromWriteRequest(WriteRequest writeRequest)
        {
            var classification = new Classification();
            classification.Artist = writeRequest.PutRequest.Item["artist"].S;
            classification.PageId = int.Parse(writeRequest.PutRequest.Item["pageId"].N);

            if (writeRequest.PutRequest.Item.ContainsKey("date"))
            {
                classification.Date = writeRequest.PutRequest.Item["date"].S;
            }
            if (writeRequest.PutRequest.Item.ContainsKey("name"))
            {
                classification.Name = writeRequest.PutRequest.Item["name"].S;
            }
            if (writeRequest.PutRequest.Item.ContainsKey("imageId"))
            {
                classification.ImageId = int.Parse(writeRequest.PutRequest.Item["imageId"].N);
            }

            return classification;
        }

        //[TestCase]
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

        List<Classification> classifications = new List<Classification>();

        //[Test]
        public void B_Pull_Records_With_ID()
        {
            string[] files = Directory.GetFiles(DataDump.CLASSIFICATION_ARCHIVE);

            foreach (var fileName in files)
            {
                string rawPageId = fileName
                    .Replace(DataDump.CLASSIFICATION_ARCHIVE + "\\", String.Empty)
                    .Replace(DataDump.FILE_IDENTITY_TEMPLATE, string.Empty)
                    .Replace(".json", string.Empty);
                int pageId = int.Parse(rawPageId);

                var jsonClassifiction = File.ReadAllText(fileName);
                var classifiation = JsonConvert.DeserializeObject<Classification>(jsonClassifiction);
                classifiation.PageId = pageId;
                classifications.Add(classifiation);
            }
        }

        readonly List<List<Classification>> classificationBatches = new List<List<Classification>>();

        //[Test]
        public void C_Group_Into_Batches()
        {
            while (classifications.Any())
            {
                classificationBatches.Add(classifications.Take(batchSize).ToList());
                classifications = classifications.Skip(batchSize).ToList();
            }

        }

        //[Test]
        public void D_Insert_In_Bulk()
        { 
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();

            for (var index = 0; index < classificationBatches.Count; index += 1)
            {
                var batchOfClassifications = classificationBatches[index];
                File.WriteAllText("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\DynamoDbProgress.txt", JsonConvert.SerializeObject(batchOfClassifications));
                BatchWriteItemResponse batchWriteResponse = client.BatchWriteItem(GetBatchWriteRequest(batchOfClassifications));

                if (batchWriteResponse.UnprocessedItems.Count > 0)
                {
                    List<Classification> failedClassifications = new List<Classification>();
                    foreach (var failedWriteRequest in batchWriteResponse.UnprocessedItems[request.TableName])
                    {
                        failedClassifications.Add(GetClassificationFromWriteRequest(failedWriteRequest));
                    }

                    classificationBatches.Add(failedClassifications);

                    File.WriteAllText("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\DynamoDbProgressOfFailures.txt",
                        $"Encountered {failedClassifications.Count} failures on. On batch number {index} of {classificationBatches.Count} batches");
                }
            }
        }

        [Test]
        public void Check_Count()
        {
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var tableDescription = client.DescribeTable(request.TableName);
            Console.WriteLine(tableDescription.Table.ItemCount);

            Assert.AreEqual(288106, tableDescription.Table.ItemCount); // That feels good. Perfect run with error handling and retrying records.
        }

    }
}
