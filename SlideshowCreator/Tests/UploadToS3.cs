using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.SQS.Model;
using ArtApi.Model;
using AwsLambdaDeploy;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.Sources.MetropolitanMuseumOfArt;
using IndexBackend.Sources.MinistereDeLaCulture;
using IndexBackend.Sources.MuseumOfModernArt;
using IndexBackend.Sources.Rijksmuseum;
using IndexBackend.Sources.Rijksmuseum.Model;
using JetBrains.dotMemoryUnit;
using Newtonsoft.Json;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;

namespace SlideshowCreator.Tests
{
    class UploadToS3
    {
        public static List<RegionEndpoint> Regions => RegionEndpoint.EnumerableAllRegions
            .Where(x =>
                        x != RegionEndpoint.USGovCloudWest1 && // Government regions are restricted
                        x != RegionEndpoint.USGovCloudEast1 &&
                        !string.Equals(x.SystemName, "us-isob-east-1", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(x.SystemName, "us-iso-east-1", StringComparison.OrdinalIgnoreCase) &&
                        x != RegionEndpoint.CNNorth1 && // I assume some sites can't be accessed behind the "Great Firewall" of China
                        x != RegionEndpoint.CNNorthWest1 &&
                        x != RegionEndpoint.AFSouth1 && // Some of these regions can be enabled in the billing console: https://console.aws.amazon.com/billing/home?#/account?AWS-Regions
                        x != RegionEndpoint.APEast1 &&
                        x != RegionEndpoint.EUSouth1 &&
                        x != RegionEndpoint.MESouth1).ToList();
        private const string FUNCTION_INDEX_AD_HOME = "gonzalez-art-foundation-crawler";
        
        [Test]
        public void DeleteAllFunctions()
        {
            new LambdaDeploy().Delete(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                Regions,
                FUNCTION_INDEX_AD_HOME,
                100
            );
        }

        //[Test] - The table should probably be rebuilt maybe once per week to keep up with any data changes.
        public void BuildArtistList()
        {
            var artists = new Dictionary<string, string>();
            var request = new ScanRequest(new ClassificationModel().GetTable());
            var dynamoDbClient = GalleryAwsCredentialsFactory.ProductionDbClient;
            ScanResponse response = null;
            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }
                response = dynamoDbClient.ScanAsync(request).Result;
                foreach (var item in response.Items)
                {
                    var model = JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(item).ToJson());
                    if (string.IsNullOrWhiteSpace(model.Artist))
                    {
                        Console.WriteLine("No artist");
                    }
                    if (!string.IsNullOrWhiteSpace(model.Artist) &&
                        !artists.ContainsKey(model.Artist))
                    {
                        artists.Add(model.Artist, model.OriginalArtist);
                        var artistModel = new ArtistModel
                        {
                            Artist = model.Artist,
                            OriginalArtist = model.OriginalArtist
                        };
                        var json = JsonConvert.SerializeObject(artistModel, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        var result = dynamoDbClient.PutItemAsync(
                            new ArtistModel().GetTable(),
                            Document.FromJson(json).ToAttributeMap()
                        ).Result;
                    }
                }
            } while (response.LastEvaluatedKey.Any());
        }
        
        [Test]
        public void DeployArtIndexerInEachRegion()
        {
            var environmentVariables = new Dictionary<string, string>
            {
                { "ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN", Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN") },
                { "ELASTICSEARCH_API_ENDPOINT_FOUNDATION", Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION") },
                { "RIJKSMUSEUM_DATA_API_KEY", Environment.GetEnvironmentVariable("RIJKSMUSEUM_DATA_API_KEY") }
            };

            var scheduledFrequencyInMinutes = 15;
            var increment = scheduledFrequencyInMinutes == 1 ? "minute" : "minutes";
            var scheduleExpression = $"rate({scheduledFrequencyInMinutes} {increment})";

            new LambdaDeploy().Deploy(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                new List<RegionEndpoint>
                {
                    RegionEndpoint.USEast1
                },
                environmentVariables,
                scheduleExpression,
                FUNCTION_INDEX_AD_HOME,
                @"C:\Users\peon\Desktop\projects\gonzalez-art-foundation-api\ArtIndexer\ArtIndexer.csproj",
                new LambdaEntrypointDefinition
                {
                    AssemblyName = "ArtIndexer",
                    Namespace = "ArtIndexer",
                    ClassName = "Function",
                    FunctionName = "FunctionHandler"
                },
                roleArn: "arn:aws:iam::283733643774:role/lambda_exec_art_api",
                runtime: Runtime.Dotnetcore31,
                1024*6,
                1,
                TimeSpan.FromMinutes(scheduledFrequencyInMinutes));
        }

        [Test]
        public void RetryQueue()
        {
            var sqsClient = GalleryAwsCredentialsFactory.SqsClient;

            ReceiveMessageResponse messages;
            do
            {
                var receiveRequest =
                    new ReceiveMessageRequest(QueueIndexer.QUEUE_URL)
                    {
                        MaxNumberOfMessages = 10
                    };
                messages = sqsClient.ReceiveMessageAsync(receiveRequest).Result;

                if (!messages.Messages.Any())
                {
                    break;
                }

                var sendMessages = new List<SendMessageBatchRequestEntry>();
                foreach (var message in messages.Messages)
                {
                    sendMessages.Add(new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), message.Body));
                }

                var result = sqsClient.SendMessageBatchAsync(QueueIndexer.QUEUE_FAILURE_URL, sendMessages).Result;
                if (result.Failed.Any())
                {
                    throw new Exception("Failed to retry sqs message.");
                }

                var deleteMessages = new List<DeleteMessageBatchRequestEntry>();
                foreach (var message in messages.Messages)
                {
                    deleteMessages.Add(new DeleteMessageBatchRequestEntry(Guid.NewGuid().ToString(), message.ReceiptHandle));
                }
                sqsClient.DeleteMessageBatchAsync("https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler-failure", deleteMessages).Wait();
            } while (messages.Messages.Any());
        }

        //[Test]
        public void TestIndex()
        {
            //var client = new HttpClient();
            //var indexer = new IndexerFactory().GetIndexer(NationalGalleryOfArtIndexer.Source, client, null, null);
            //var result = indexer.Index("100515", null).Result;
            //Console.WriteLine(result.Model.Source);
            //Console.WriteLine(result.Model.PageId); 
        }
        
        //[Test]
        public void HarvestMetropolitanMuseumOfArtPages()
        {
            var harvester = new IndexBackend.Sources.MetropolitanMuseumOfArt.Harvester();
            harvester.Harvest(GalleryAwsCredentialsFactory.SqsClient, @"C:\Users\peon\Downloads\MetObjects.csv");
        }

        //[Test]
        public void HarvestMomaPages()
        {
            var sqsClient = GalleryAwsCredentialsFactory.SqsClient;
            const int pageIdLimit = 600000;
            for (var ct = 299976; ct < pageIdLimit; ct += 10)
            {
                var batch = new List<ClassificationModel>();
                for (var pageId = ct; pageId < ct + 10 && pageId < pageIdLimit; pageId++)
                {
                    batch.Add(new ClassificationModel
                    {
                        Source = MuseumOfModernArtIndexer.Source,
                        PageId = pageId.ToString()
                    });
                }

                IndexBackend.Harvester.SendBatch(
                    sqsClient,
                    "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler",
                    batch
                        .Select(crawlerModel =>
                            new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), JsonConvert.SerializeObject(crawlerModel)))
                        .ToList()
                );
            }
        }

        [Test]
        public void Count()
        {
            var queryRequest = new QueryRequest(new ClassificationModel().GetTable())
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":source", new AttributeValue {S = MetropolitanMuseumOfArtIndexer.Source}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#source", "source"}
                },
                KeyConditionExpression = "#source = :source"
            };
            var results = QueryAll<ClassificationModel>(queryRequest, GalleryAwsCredentialsFactory.ProductionDbClient)
                .OrderByDescending(x => int.Parse(x.PageId))
                .ToList();

            Console.WriteLine(results.Count.ToString());
        }

        [Test]
        public void HarvestMinisreDeCulturePageIds()
        {
            var queryRequest = new QueryRequest(new ClassificationModel().GetTable())
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":source", new AttributeValue {S = MinistereDeLaCultureIndexer.SourceMinistereDeLaCulture}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#source", "source"}
                },
                KeyConditionExpression = "#source = :source"
            };
            var results = QueryAll<ClassificationModel>(queryRequest, GalleryAwsCredentialsFactory.ProductionDbClient)
                .ToDictionary(x => x.PageId);
            var lines = File.ReadAllText(@"C:\Users\peon\Downloads\base-joconde-extrait.json");
            var json = JsonConvert.DeserializeObject<List<MonaLisaDatabaseModel>>(lines)
                .Where(x =>
                    x.Fields != null &&
                    (x.Fields.Domn ?? string.Empty).ToLower().Contains("peinture") &&
                    !string.Equals(x.Fields.Museo, "m5031", StringComparison.OrdinalIgnoreCase) &&
                    !results.ContainsKey(x.Fields.Ref)
                )
                .Select(x =>
                    new ClassificationModel
                    {
                        Source = MinistereDeLaCultureIndexer.SourceMinistereDeLaCulture,
                        PageId = x.Fields.Ref
                    })
                .ToList();

            var sqsClient = GalleryAwsCredentialsFactory.SqsClient;
            var crawlerJsonBatches = Batcher.Batch(10, json);
            Console.WriteLine($"Sending {crawlerJsonBatches.Count} batches.");

            Parallel.ForEach(crawlerJsonBatches, crawlerJsonBatch =>
            {
                Console.WriteLine($"Sending {crawlerJsonBatch.Count} messages.");
                IndexBackend.Harvester.SendBatch(
                    sqsClient,
                    "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler",
                    crawlerJsonBatch
                        .Select(crawlerJson => new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), JsonConvert.SerializeObject(crawlerJson, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore})))
                        .ToList()
                );
            });
        }

        public List<T> QueryAll<T>(QueryRequest queryRequest, IAmazonDynamoDB client, int limit = 0)
        {
            QueryResponse queryResponse = null;
            var items = new List<T>();
            do
            {
                if (queryResponse != null)
                {
                    queryRequest.ExclusiveStartKey = queryResponse.LastEvaluatedKey;
                }
                queryResponse = client.QueryAsync(queryRequest).Result;
                foreach (var item in queryResponse.Items)
                {
                    if (limit > 0 && items.Count >= limit)
                    {
                        return items;
                    }
                    items.Add(JsonConvert.DeserializeObject<T>(Document.FromAttributeMap(item).ToJson()));
                }
            } while (queryResponse.LastEvaluatedKey.Any());
            return items;
        }

        //[Test]
        public void SendToElastic()
        {
            var client = new HttpClient();

            var queryRequest = new ScanRequest(new ClassificationModel().GetTable());
            var dbClient = GalleryAwsCredentialsFactory.ProductionDbClient;
            var elasticClient = new ElasticSearchClient(
                client,
                Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION"),
                Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN"));

            ScanResponse queryResponse = null;
            queryResponse = new ScanResponse();
            queryResponse.LastEvaluatedKey = new Dictionary<string, AttributeValue>
            {
                { "source", new AttributeValue { S = "https://www.pop.culture.gouv.fr/notice/museo/M5031" } },
                { "pageId", new AttributeValue { S = "50350123823" } }
            };
            do
            {
                if (queryResponse != null)
                {
                    queryRequest.ExclusiveStartKey = queryResponse.LastEvaluatedKey;
                }
                queryResponse = dbClient.ScanAsync(queryRequest).Result;
                var status = "Sending batch from start key: " + JsonConvert.SerializeObject(queryRequest.ExclusiveStartKey);
                Console.WriteLine(status);
                var batch = new List<ClassificationModel>();
                foreach (var item in queryResponse.Items)
                {
                    var classificationModel = JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(item).ToJson());
                    batch.Add(classificationModel);
                }
                Parallel.ForEach(batch, new ParallelOptions { MaxDegreeOfParallelism = 10 }, classification =>
                {
                    elasticClient.SendToElasticSearch(classification).Wait();
                });
               Thread.Sleep(500);
            } while (queryResponse.LastEvaluatedKey.Any());

        }

        [Test]
        public void HarvestRikjsmuseumPageIds()
        {
            var apiKey = Environment.GetEnvironmentVariable("RIJKSMUSEUM_DATA_API_KEY");
            new IndexBackend.Sources.Rijksmuseum.Harvester(GalleryAwsCredentialsFactory.SqsClient, apiKey).Harvest().Wait();
        }

        [Test] //Keep for debugging until everything is crawled.
        public void IndexSingleRecord()
        {
            var indexingCore = new IndexingCore(
                GalleryAwsCredentialsFactory.ProductionDbClient,
                GalleryAwsCredentialsFactory.S3Client,
                GalleryAwsCredentialsFactory.ElasticSearchClient,
                GalleryAwsCredentialsFactory.RekognitionClientClient);
            var indexer = new RijksmuseumIndexer(new HttpClient(), new ConsoleLogging());
            indexingCore.Index(indexer, new ClassificationModel { Source = RijksmuseumIndexer.Source, PageId = "BK-1954-43-1" }).Wait();
        }

        [Test] //Keep for debugging until everything is crawled.
        public void RunQueueLocally()
        {
            var indexingCore = new IndexingCore(
                GalleryAwsCredentialsFactory.ProductionDbClient,
                GalleryAwsCredentialsFactory.S3Client,
                GalleryAwsCredentialsFactory.ElasticSearchClient,
                GalleryAwsCredentialsFactory.RekognitionClientClient);
            var queueIndexer = new QueueIndexer(GalleryAwsCredentialsFactory.SqsClient,
                new HttpClient(),
                indexingCore,
                new ConsoleLogging());
            queueIndexer.ProcessAllInQueue(4);
            dotMemory.Check(memory =>
            {
                Console.WriteLine("bytes persisting: " + memory.SizeInBytes / 1024 / 1024 + "MB");
                Console.WriteLine("Array MB in memory: " + memory.GetObjects(where => where.Type.Is<byte[]>()).ObjectsCount);
                dotMemory.Check(memory => Assert.AreEqual(0, memory.GetObjects(where => where.Type.Is<IndexResult>()).ObjectsCount));
                dotMemory.Check(memory => Assert.AreEqual(1, memory.GetObjects(where => where.Type.Is<MemoryStream>()).ObjectsCount));
                dotMemory.Check(memory => Assert.AreEqual(0, memory.GetObjects(where => where.Type.Is<TileImage>()).ObjectsCount));
                dotMemory.Check(memory => Assert.AreEqual(0, memory.GetObjects(where => where.Type.Is<Image<Rgba64>>()).ObjectsCount));
                dotMemory.Check(memory => Assert.AreEqual(1, memory.GetObjects(where => where.Type.Is<ArrayPoolMemoryAllocator>()).ObjectsCount));
                Assert.LessOrEqual(memory.SizeInBytes, 512 * 1024 * 1024);
                // when memory is high, uncomment to throw error and then debug the .dmw file in UI.
                //dotMemory.Check(memory => Assert.AreEqual(1, 2));
            });
        }
    }
}
