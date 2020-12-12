using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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
                25
            );
        }

        [Test]
        public void DeployArtIndexerInEachRegion()
        {
            new LambdaDeploy().Deploy(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                new List<RegionEndpoint>
                {
                    RegionEndpoint.USEast1, // Stay in the US, because some regions can't access certain links and it's hard to tell which those are outside the US for any given region and link.
                    RegionEndpoint.USEast2  // I'm even having problems from the west coast, which is odd, because I'm crawling the image link based on the html the image is presented on so it would adjust for each region.
                },
                environmentVariables,
                15,
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
                1,
                TimeSpan.FromMinutes(15));
        }

        [Test]
        public void RetryQueue()
        {
            var sqsClient = GalleryAwsCredentialsFactory.SqsClient;

            ReceiveMessageResponse messages;
            do
            {
                var receiveRequest = new ReceiveMessageRequest("https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler-failure");
                receiveRequest.MaxNumberOfMessages = 10;
                messages = sqsClient.ReceiveMessage(receiveRequest);

                if (!messages.Messages.Any())
                {
                    break;
                }

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
        public void HarvestMomaPages()
        {
            var sqsClient = GalleryAwsCredentialsFactory.SqsClient;
            const int pageIdLimit = 600000;
            for (var ct = 299976; ct < pageIdLimit; ct += 10)
            {
                var batch = new List<QueueCrawlerModel>();
                for (var pageId = ct; pageId < ct + 10 && pageId < pageIdLimit; pageId++)
                {
                    batch.Add(new QueueCrawlerModel
                    {
                        Source = MuseumOfModernArtIndexer.Source,
                        Id = pageId.ToString()
                    });
                }

                SendBatch(
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
        public void SendToElastic()
        {
            var client = new HttpClient();

            var queryRequest = new QueryRequest(new ClassificationModel().GetTable())
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    //{":source", new AttributeValue {S = MuseumOfModernArtIndexer.Source}}
                    //{":source", new AttributeValue {S = TheAthenaeumIndexer.Source}}
                    //{":source", new AttributeValue {S = NationalGalleryOfArtIndexer.Source}}
                    //{":source", new AttributeValue {S = MuseeOrsayIndexer.Source }}
                    //{":source", new AttributeValue {S = MinistereDeLaCultureIndexer.SourceMinistereDeLaCulture }}
                    //{":source", new AttributeValue {S = MinistereDeLaCultureIndexer.SourceMuseeDuLouvre }}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#source", "source"}
                },
                KeyConditionExpression = "#source = :source"
            };
            var dbClient = GalleryAwsCredentialsFactory.ProductionDbClient;

            QueryResponse queryResponse = null;
            do
            {
                if (queryResponse != null)
                {
                    queryRequest.ExclusiveStartKey = queryResponse.LastEvaluatedKey;
                }
                queryResponse = dbClient.QueryAsync(queryRequest).Result;
                var status = "Sending batch from start key: " + JsonConvert.SerializeObject(queryRequest.ExclusiveStartKey);
                Console.WriteLine(status);
                var batch = new List<ClassificationModel>();
                foreach (var item in queryResponse.Items)
                {
                    var classificationModel = JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(item).ToJson());
                    batch.Add(classificationModel);
                }

                Parallel.ForEach(batch, new ParallelOptions { MaxDegreeOfParallelism = 20 }, classification =>
                {
                    SendToElasticSearch(client, classification);
                });

            } while (queryResponse.LastEvaluatedKey.Any());

        }

        [Test]
        public void TestSearch()
        {
            var text = "musee-orsay";
            var getRequest = $@"{{
  ""query"": {{
    ""multi_match"" : {{
      ""query"":    ""{text}"", 
      ""fields"": [
        ""artist"",
        ""name"",
        ""date"",
        ""source"",
        ""sourceLink""
      ]
    }}
  }},
  ""size"": 1000
}}";

            var handler = new WinHttpHandler();

            var response = SendToElasticSearch(
                new HttpClient(handler),
                HttpMethod.Get,
                "/classification/_search",
                JObject.Parse(getRequest));

            var responseJson = JObject.Parse(response);
            var items = responseJson["hits"]["hits"].Select(x => x["_source"]).ToList();
            Console.WriteLine(JsonConvert.SerializeObject(items, Formatting.Indented));
        }

        public void SendToElasticSearch(HttpClient client, ClassificationModel classification)
        {
            var json = JObject.Parse(JsonConvert.SerializeObject(classification, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            json.Add("@timestamp", DateTime.UtcNow.ToString("O"));
            var path = "/classification/_doc/" + HttpUtility.UrlEncode($"{classification.Source}:{classification.PageId}");
            SendToElasticSearch(client, HttpMethod.Post, path, json);
        }

        public string SendToElasticSearch(HttpClient client, HttpMethod method, string path, JObject json)
        {
            var apiKey = Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN");

            var request = new HttpRequestMessage(
                method,
                new Uri($"{Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION")}{path}"));
            if (json != null)
            {
                request.Content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
            }
            request.Headers.Add("Authorization", "ApiKey " + apiKey);

            var response = client.SendAsync(request).Result;

            if (!response.IsSuccessStatusCode)
            {
                var dataBack = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine(dataBack);
            }
            response.EnsureSuccessStatusCode();
            var responseText = response.Content.ReadAsStringAsync().Result;
            return responseText;
        }

        [Test]
        public void Count()
        {
            var queryRequest = new QueryRequest(new ClassificationModel().GetTable())
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":source", new AttributeValue {S = MuseumOfModernArtIndexer.Source}}
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    {"#source", "source"}
                },
                KeyConditionExpression = "#source = :source"
            };
            var results = QueryAll<ClassificationModel>(queryRequest, GalleryAwsCredentialsFactory.ProductionDbClient)
                .OrderByDescending(x => int.Parse(x.PageId))
                .Where(x => x.Name.Contains("&#"))
                .ToList();


            Console.WriteLine(results.Count.ToString());
            //Console.WriteLine(JsonConvert.SerializeObject(results, Formatting.Indented));
            //return;
            var sqsClient = GalleryAwsCredentialsFactory.SqsClient;


            var crawlerJsonBatches = Batcher.Batch(10, results);
            Parallel.ForEach(crawlerJsonBatches, crawlerJsonBatch =>
            {
                Console.WriteLine($"Sending {crawlerJsonBatch.Count} messages.");
                SendBatch(
                    sqsClient,
                    "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler",
                    crawlerJsonBatch
                        .Select(crawlerJson =>
                            new SendMessageBatchRequestEntry(
                                Guid.NewGuid().ToString(),
                                JsonConvert.SerializeObject(new QueueCrawlerModel { Id = crawlerJson.PageId, Source = crawlerJson.Source })
                            )
                        )
                        .ToList()
                );
            });
        }

        [Test]
        public void TestIndex()
        {
            var indexer = new MuseumOfModernArtIndexer(
                GalleryAwsCredentialsFactory.S3AcceleratedClient,
                new HttpClient(),
                new ConsoleLogging()
            );
            var classification = indexer.Index("298552").Result;
            classification.Name = HttpUtility.HtmlDecode(classification.Name);
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

            var sqsClient = GalleryAwsCredentialsFactory.SqsClient;
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
