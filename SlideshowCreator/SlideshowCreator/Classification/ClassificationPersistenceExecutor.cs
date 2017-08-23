using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SlideshowCreator.Classification
{
    class ClassificationPersistenceExecutor
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

        private Dictionary<string, AttributeValue> ConvertToNameValuePair(ClassificationModel classification)
        {
            var kvp = new Dictionary<string, AttributeValue>();
            kvp.Add("pageId", new AttributeValue { N = classification.PageId.ToString() });

            kvp.Add("artist", new AttributeValue { S = string.IsNullOrWhiteSpace(classification.Artist)
                ? Classifier.UNKNOWN_ARTIST
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


        public Dictionary<string, List<WriteRequest>> GetBatchWriteRequest(List<ClassificationModel> classifications)
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
        private ClassificationModel GetClassificationFromWriteRequest(WriteRequest writeRequest)
        {
            var classification = new ClassificationModel();
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
        public void AA_Drop_Then_Create_Table()
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

        List<ClassificationModel> classifications = new List<ClassificationModel>();

        //[Test]
        public void B_Pull_Records_With_ID()
        {
            string[] files = Directory.GetFiles(PublicConfig.ClassificationArchive);

            foreach (var fileName in files)
            {
                string rawPageId = fileName
                    .Replace(PublicConfig.ClassificationArchive + "\\", String.Empty)
                    .Replace(Crawler.FILE_IDENTITY_TEMPLATE, string.Empty)
                    .Replace(".json", string.Empty);
                int pageId = int.Parse(rawPageId);

                var jsonClassifiction = File.ReadAllText(fileName);
                var classifiation = JsonConvert.DeserializeObject<ClassificationModel>(jsonClassifiction);
                classifiation.PageId = pageId;
                classifications.Add(classifiation);
            }
        }

        readonly List<List<ClassificationModel>> classificationBatches = new List<List<ClassificationModel>>();

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
                    List<ClassificationModel> failedClassifications = new List<ClassificationModel>();
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

            Assert.AreEqual(288105, tableDescription.Table.ItemCount); // Now only off by 1 record. I have no clue what happened! I'll fix the existence of this problem with a backup after re-indexing.
        }

        [Test]
        public void Classify_Page_With_E_Acute_Transiently()
        {
            var privateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");
            int pageId = 165488;
            var html = Crawler.GetDetailsPageHtml(privateConfig.TargetUrl, pageId, privateConfig.PageNotFoundIndicatorText);
            var classification = new Classifier().Classify(html, pageId);

            Assert.AreEqual(pageId, classification.PageId);
            Assert.AreEqual(524911, classification.ImageId);

            Assert.AreEqual("The Sleeping Beauty: The Aged King Pleads with the Good Fairy", classification.Name);
            Assert.AreEqual("Léon Bakst", classification.Artist);
            Assert.AreEqual("1913-1922", classification.Date);
        }
        
        //[Test]
        public void Find_All_E_Acute_Artists()
        {
            var client = new DynamoDbClientFactory().Create(); // Lookup the old artists name.
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var scanRequest = new ScanRequest(request.TableName);
            scanRequest.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":artist", new AttributeValue { S = "Ã©" } }
            };
            scanRequest.FilterExpression = "contains(artist, :artist)";

            ScanResponse scanResponse = null;

            var allMatches = new List<Dictionary<string, AttributeValue>>();
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanResponse = client.Scan(scanRequest);

                if (scanResponse.Items.Any())
                {
                    allMatches.AddRange(scanResponse.Items);
                }
            } while (scanResponse.LastEvaluatedKey.Any());
            
            Console.WriteLine("Reclasiffying: " + allMatches.Count);

            foreach (var matchingItem in allMatches)
            {
                Reclassify_Transiently(int.Parse(matchingItem["pageId"].N));
            }
        }

        public void Reclassify_Transiently(int pageId)
        {
            var privateConfig = PrivateConfig.Create("C:\\Users\\peon\\Desktop\\projects\\SlideshowCreator\\personal.json");
            var html = Crawler.GetDetailsPageHtml(privateConfig.TargetUrl, pageId, privateConfig.PageNotFoundIndicatorText);
            var classification = new Classifier().Classify(html, pageId);

            var client = new DynamoDbClientFactory().Create(); // Lookup the old artists name.
            var request = new DynamoDbTableFactory().GetTableDefinition();
            var queryRequest = new QueryRequest
            {
                TableName = request.TableName,
                KeyConditionExpression = "pageId = :v_pageId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":v_pageId", new AttributeValue { N = classification.PageId.ToString() }}}
            };
            var queryResponse = client.Query(queryRequest);
            var existingRecord = queryResponse.Items.SingleOrDefault();

            if (existingRecord != null)
            {
                existingRecord = new Dictionary<string, AttributeValue>
                {
                    {"pageId", new AttributeValue {N = existingRecord["pageId"].N}},
                    {"artist", new AttributeValue {S = existingRecord["artist"].S}}
                };
                client.DeleteItem(request.TableName, existingRecord);
            }

            var refreshedNvp = ConvertToNameValuePair(classification);
            client.PutItem(request.TableName, refreshedNvp);

            Console.WriteLine(pageId);
            Check_Count();
        }

        // These can move to the web app tomorrow.
        // The web app is going to need a "Global Secondary Index" on artist for this to work for a user.

        [Test]
        public void Find_All_For_Artist()
        {
            var client = new DynamoDbClientFactory().Create(); // Lookup the old artists name.
            var request = new DynamoDbTableFactory().GetTableDefinition();

            //var artistNameIs = "eon Gérô";
            var artistNameIs = "Jean-Leon Gerome"; // I've got to make this work. Pretty much no search is accent insensitive. Even elastic search reccomends to index twice with and without diatrics.

            // https://stackoverflow.com/questions/5459641/replacing-characters-in-c-sharp-ascii/13154805#13154805
            // I'll have to rip through everything again.
            // I may as well re-classify transiently, add a new field called originalArtistName which is just a plain field for display.
            // For search I will use the simple ascii.
            // 
            // pageId (partitionKey)
            // artist (sortKey - stripped of diatrics)
            // imageId
            // name
            // date
            // originalArtist

            // The gallery must show the original artist name, out of respect for the artist.

            // Then just add a global seconary index on artist.

            // Ugh, that's a few days work. To speed it up I will just grab all, then update all, risking data loss within dynamodb is fine.
            // Just fixing the data in dynamodb should cut the work down to a day.
            // Then I can work on the queries for the interface...and finally the interface this weekend.
            // Should be really quick, I just need something slightly better than Microsofts and My TV's ridiculous image viewer's.
            
            // I kind of want to move quick now to start on nga.gov and set up my "requester".

            var scanRequest = new ScanRequest(request.TableName);
            scanRequest.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":artist", new AttributeValue { S = artistNameIs } }
            };
            scanRequest.FilterExpression = "contains(artist, :artist)";

            ScanResponse scanResponse = null;

            var allMatches = new List<Dictionary<string, AttributeValue>>();
            do
            {
                if (scanResponse != null)
                {
                    scanRequest.ExclusiveStartKey = scanResponse.LastEvaluatedKey;
                }
                scanResponse = client.Scan(scanRequest);

                if (scanResponse.Items.Any())
                {
                    allMatches.AddRange(scanResponse.Items);
                }
            } while (scanResponse.LastEvaluatedKey.Any());

            Console.WriteLine("Matches: " + allMatches.Count); // No results right now without accents.
        }

    }
}
