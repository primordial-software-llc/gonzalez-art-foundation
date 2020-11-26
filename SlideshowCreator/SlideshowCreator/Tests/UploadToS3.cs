using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.WebSockets;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Internal;
using Amazon.Lambda;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Amazon.SQS;
using Amazon.SQS.Model;
using AwsTools;
using GalleryBackend;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.LambdaSymphony;
using IndexBackend.MinistereDeLaCulture;
using IndexBackend.MuseeOrsay;
using IndexBackend.MuseumOfModernArt;
using IndexBackend.NationalGalleryOfArt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace SlideshowCreator.Tests
{
    class UploadToS3
    {

        public const string BUCKET = "gonzalez-art-foundation";

        public static AWSCredentials CreateCredentialsTest()
        {
            var chain = new CredentialProfileStoreChain();
            var profile = "test";
            if (!chain.TryGetAWSCredentials(profile, out AWSCredentials awsCredentials))
            {
                throw new Exception($"AWS credentials not found for \"{profile}\" profile.");
            }
            return awsCredentials;
        }

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
        private readonly Dictionary<string, string> environmentVariables = new Dictionary<string, string>();
        private const string FUNCTION_INDEX_AD_HOME = "gonzalez-art-foundation-crawler";

        [Test]
        public void DeleteFunctions()
        {
            new LambdaDeploy().Delete(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                Regions,
                FUNCTION_INDEX_AD_HOME,
                5
            );
        }

        [Test]
        public void DeployArtIndexerInEachRegion()
        {
            new LambdaDeploy().Deploy(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                new List<RegionEndpoint> { RegionEndpoint.USEast1 }, // Some regions can't access the image url which happens to be an aws bucket.
                environmentVariables,
                5,
                FUNCTION_INDEX_AD_HOME,
                @"C:\Users\peon\Desktop\projects\gonzalez-art-foundation-api\SlideshowCreator\ArtIndexer\ArtIndexer.csproj",
                new LambdaEntrypointDefinition
                {
                    AssemblyName = "ArtIndexer",
                    Namespace = "ArtIndexer",
                    ClassName = "Function",
                    FunctionName = "FunctionHandler"
                },
                roleArn: "arn:aws:iam::283733643774:role/lambda_exec_art_api",
                runtime: Runtime.Dotnetcore31,
                256,
                5);
        }

        [Test]
        public void RetryQueue()
        {
            var sqsClient = new AmazonSQSClient(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USEast1 });

            ReceiveMessageResponse messages;
            do
            {
                var receiveRequest = new ReceiveMessageRequest("https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler-failure");
                receiveRequest.MaxNumberOfMessages = 10;
                messages = sqsClient.ReceiveMessage(receiveRequest);

                var sendMessages = new List<SendMessageBatchRequestEntry>();
                foreach (var message in messages.Messages)
                {
                    sendMessages.Add(new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), message.Body));
                }

                var result = sqsClient.SendMessageBatch("https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler", sendMessages);
                if (result.Failed.Any())
                {
                    throw new Exception("Failed to retry sqs message.");
                }

                var deleteMessages = new List<DeleteMessageBatchRequestEntry>();
                foreach (var message in messages.Messages)
                {
                    deleteMessages.Add(new DeleteMessageBatchRequestEntry(Guid.NewGuid().ToString(), message.ReceiptHandle));
                }
                sqsClient.DeleteMessageBatch("https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler-failure", deleteMessages);
            } while (messages.Messages.Any());
        }

        [Test]
        public void TestIndex()
        {
            var indexer = new MuseumOfModernArtIndexer(
                GalleryAwsCredentialsFactory.S3AcceleratedClient,
                new HttpClient(),
                new ConsoleLogging()
            );
            var results = indexer.Index("79802").Result;
        }

        [Test]
        public void HarvestMuseumOfModernArtPageIds()
        {
            var sqsClient = new AmazonSQSClient(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USEast1 });

            var json = new List<QueueCrawlerModel>();
            for (var ct = 1; ct < 300000; ct++)
            {
                json.Add(new QueueCrawlerModel
                {
                    Id = ct.ToString(),
                    Source = MuseumOfModernArtIndexer.Source
                });
                if (ct % 10 == 0)
                {
                    Console.WriteLine($"Sending {json.Count} messages.");
                    SendBatch(
                        sqsClient,
                        "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler",
                        json
                            .Select(queueCrawlerModel => new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), JsonConvert.SerializeObject(queueCrawlerModel)))
                            .ToList()
                    );
                    json.Clear();
                }
            }
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
                    new JObject
                    {
                        {"source", MinistereDeLaCultureIndexer.SourceMinistereDeLaCulture},
                        {"id", x.Fields.Ref}
                    })
                .ToList();

            var sqsClient = new AmazonSQSClient(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USEast1 });
            var crawlerJsonBatches = Batcher.Batch(10, json);
            Console.WriteLine($"Sending {crawlerJsonBatches.Count} batches.");

            Parallel.ForEach(crawlerJsonBatches, crawlerJsonBatch =>
            {
                Console.WriteLine($"Sending {crawlerJsonBatch.Count} messages.");
                SendBatch(
                    sqsClient,
                    "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler",
                    crawlerJsonBatch
                        .Select(crawlerJson => new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), crawlerJson.ToString()))
                        .ToList()
                );
            });
        }

        //[Test]
        public void HarvestMuseeOrsayPageIds()
        {
            var sqsClient = new AmazonSQSClient(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USEast1 });

            for (var id = 128404; id < 300000; id += 10)
            {
                Console.WriteLine("Sending batch for id starting at: " + id);
                var batch = new List<JObject>();
                for (var currentId = id; currentId < id + 10; currentId++)
                {
                    batch.Add(new JObject { {"id", currentId.ToString()} });
                }
                Console.WriteLine($"Sending {batch.Count} messages");
                SendBatch(
                    sqsClient,
                    "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler",
                    batch
                        .Select(JsonConvert.SerializeObject)
                        .Select(jsonAd => new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), jsonAd))
                        .ToList()
                );
            }
        }

        private string GetFailureReason(List<BatchResultErrorEntry> sqsFailures)
        {
            return string.Join(", ", sqsFailures.Select(x => $"Message: {x.Message} Sender's Fault: {x.SenderFault} Code: {x.Code}"));
        }

        public SendMessageBatchResponse SendBatch(IAmazonSQS queueClient, string queueUrl, List<SendMessageBatchRequestEntry> messages)
        {
            var sqsResult = queueClient.SendMessageBatchAsync(queueUrl, messages).Result;
            if (sqsResult.Failed.Any())
            {
                Console.WriteLine($"Failed to insert into SQS: {GetFailureReason(sqsResult.Failed)}.");
                Console.WriteLine("Retrying in 10 seconds");
                Thread.Sleep(TimeSpan.FromSeconds(10));

                var retryMessages = new List<SendMessageBatchRequestEntry>();
                foreach (BatchResultErrorEntry failedMessage in sqsResult.Failed)
                {
                    retryMessages.Add(messages.Single(x => x.Id == failedMessage.Id));
                }

                var retrySqsResult = queueClient.SendMessageBatchAsync(queueUrl, retryMessages).Result;
                if (retrySqsResult.Failed.Any())
                {
                    throw new Exception($"Failed to insert into SQS: {GetFailureReason(retrySqsResult.Failed)}.");
                }
            }
            return sqsResult;
        }

        //[Test]
        public void IndexNga()
        {
            var queryRequest = new QueryRequest(new ClassificationModel().GetTable())
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":source", new AttributeValue {S = NationalGalleryOfArtIndexer.Source}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#source", "source"}
                },
                KeyConditionExpression = "#source = :source"
            };

            var results = QueryAll<ClassificationModel>(queryRequest, GalleryAwsCredentialsFactory.ProductionDbClient)
                .Where(x => x.S3Path.StartsWith("gonzalez-art-foundation"))
                .ToList();
            Console.WriteLine($"Found {results.Count} records that need images.");

            Parallel.ForEach(results, new ParallelOptions {MaxDegreeOfParallelism = 2}, result =>
            {
                try
                {
                    var ngaDataAccess = new NationalGalleryOfArtDataAccess(PublicConfig.NationalGalleryOfArtUri);
                    var indexer = new NationalGalleryOfArtIndexer(GalleryAwsCredentialsFactory.S3AcceleratedClient, GalleryAwsCredentialsFactory.ProductionDbClient, ngaDataAccess);
                    var classification = indexer.Index(result.PageId);
                    if (classification == null)
                    {
                        Console.WriteLine($"No image found for page id {result.PageId}");
                    }
                    else
                    {
                        Console.WriteLine($"Uploaded image for page id {result.PageId}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Thread.Sleep(1000);
                }
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
    }
}
