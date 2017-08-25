﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Diacritics.Extensions;
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

        /// <summary>
        /// Something is wrong with the de-serialization of the values into the write requests.
        /// </summary>
        /// <returns></returns>
        private ClassificationModel GetClassificationFromWriteRequest(WriteRequest writeRequest)
        {
            return new ClassificationConversion()
                .ConvertToPoco(writeRequest.PutRequest.Item);
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

        /// <summary>
        /// I have some work to do re-indexing.
        /// </summary>
        [Test]
        public void Check_Count()
        {
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var tableDescription = client.DescribeTable(request.TableName);
            Console.WriteLine(tableDescription.Table.ItemCount);

            Assert.AreEqual(288100, tableDescription.Table.ItemCount);
        }

        //[Test]
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

        // Once I'm done with fixing the existing, I want to re-classify the sample and test that new classifications squash the diacritic and use the original artist name field.
        // Very important otherwise I can't get new pages or receive updates.
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

            var refreshedNvp = new ClassificationConversion()
                .ConvertToDynamoDb(classification);
            client.PutItem(request.TableName, refreshedNvp);

            Console.WriteLine(pageId);
            Check_Count();
        }

        // These can move to the web app tomorrow.
        // The web app is going to need a "Global Secondary Index" on artist for this to work for a user.

        //[Test]
        public void Find_All_For_Artist()
        {
            var client = new DynamoDbClientFactory().Create(); // Lookup the old artists name.
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var artistNameIs = "eon Gérô";

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
        
        //[Test]
        public void Activate_Overdrive()
        {
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();
            
            var provisionedThroughputRequest = new ProvisionedThroughput(400, 5); // Estimated at $50 per month (I'm assuming this is high, because I'm setting it for a very short period of time)
            var response = client.UpdateTable(request.TableName, provisionedThroughputRequest);
            Assert.AreEqual(HttpStatusCode.OK, response.HttpStatusCode);
        }

        //[Test]
        public void Deactivate_Overdrive()
        {
            var client = new DynamoDbClientFactory().Create();
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var provisionedThroughputRequest = new ProvisionedThroughput(25, 25); // Estimated at $15 per month (I'm assuming this is high, because I'm setting it for a very short period of time)
            var response = client.UpdateTable(request.TableName, provisionedThroughputRequest);
            Assert.AreEqual(HttpStatusCode.OK, response.HttpStatusCode);
        }
        
        //[Test]
        public void Test_Squashing_Diacritic()
        {
            var client = new DynamoDbClientFactory().Create(); // Lookup the old artists name.
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var artistNameIs = "Jean-Léon Gérôme";

            var scanRequest = new ScanRequest(request.TableName);
            scanRequest.ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":artist", new AttributeValue { S = artistNameIs } }
            };
            scanRequest.FilterExpression = "artist = :artist";

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
            foreach (var match in allMatches.Select(x => new ClassificationConversion().ConvertToPoco(x)))
            {
                match.OriginalArtist = match.Artist;
                match.Artist = match.Artist.RemoveDiacritics();

                var todb = new ClassificationConversion().ConvertToDynamoDb(match);
                var backToPoco = new ClassificationConversion().ConvertToPoco(todb);

                Console.WriteLine(JsonConvert.SerializeObject(backToPoco));
            }
        }

        [Test]
        public void Rebuild_Table_Cant_Use_Local_Files_They_Have_Incorrect_Encoding_Remove_Diacritics_From_Artist_And_Store_Original_Artist_In_New_Field()
        {
            var client = new DynamoDbClientFactory().Create(); // Lookup the old artists name.
            var request = new DynamoDbTableFactory().GetTableDefinition();

            var allMatches = new List<Dictionary<string, AttributeValue>>();

            ScanResponse scanResponse = null;
            var scanRequest = new ScanRequest(request.TableName);
            scanRequest.FilterExpression = $"attribute_not_exists({ClassificationModel.ORIGINAL_ARTIST})";
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

            Parallel.ForEach(allMatches, match =>
            {
                var matchAsPoco = new ClassificationConversion().ConvertToPoco(match);
                matchAsPoco.OriginalArtist = matchAsPoco.Artist;
                matchAsPoco.Artist = matchAsPoco.Artist.RemoveDiacritics();

                Console.WriteLine("Correcting: " + JsonConvert.SerializeObject(matchAsPoco));

                var recordIdentity = new Dictionary<string, AttributeValue>
                {
                    {"pageId", new AttributeValue {N = match["pageId"].N}},
                    {"artist", new AttributeValue {S = match["artist"].S}}
                };
                client.DeleteItem(request.TableName, recordIdentity);

                var diacriticFixedDynamoDbRecord = new ClassificationConversion()
                    .ConvertToDynamoDb(matchAsPoco);
                client.PutItem(request.TableName, diacriticFixedDynamoDbRecord);
            });

            Console.WriteLine("Records updated: " + allMatches.Count);
        }

    }
}
