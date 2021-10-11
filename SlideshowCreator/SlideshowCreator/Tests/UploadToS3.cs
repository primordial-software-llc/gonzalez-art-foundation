using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS.Model;
using AwsLambdaDeploy;
using IndexBackend;
using IndexBackend.Model;
using IndexBackend.Sources.MetropolitanMuseumOfArt;
using IndexBackend.Sources.MinistereDeLaCulture;
using IndexBackend.Sources.MuseumOfModernArt;
using IndexBackend.Sources.NationalGalleryOfArt;
using IndexBackend.Sources.TheAthenaeum;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

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
        
        //[Test]
        public void DeleteAllFunctions()
        {
            new LambdaDeploy().Delete(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                Regions,
                FUNCTION_INDEX_AD_HOME,
                5
            );
        }

        [Test]
        public void FindInvalidDates() // Incredibly important for knowing what's in the public domain.
        {
            const int thresholdYear = 1924; // https://fairuse.stanford.edu/overview/public-domain/
            var request = new QueryRequest(new ClassificationModel().GetTable())
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":source", new AttributeValue { S = TheAthenaeumIndexer.Source } }
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#source", "source" }
                },
                KeyConditionExpression = "#source = :source"
            };
            var dynamoDbClient = GalleryAwsCredentialsFactory.ProductionDbClient;
            var elasticSearchClient = new ElasticSearchClient(
                new HttpClient(),
                Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION"),
                Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN"));
            QueryResponse response = null;
            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }
                response = dynamoDbClient.QueryAsync(request).Result;
                foreach (var item in response.Items)
                {
                    var model = JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(item).ToJson());
                    if (!int.TryParse(model.Date, out int parsedYear))
                    {
                        Console.WriteLine("Unknown date: " + model.Date);
                    }
                    else if (parsedYear > thresholdYear)
                    {
                        MoveForReview(dynamoDbClient, elasticSearchClient, model);
                        Thread.Sleep(200);
                    }
                }
            } while (response.LastEvaluatedKey.Any());
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
        public void HideImagesInReview()
        {
            // HAZARD TEST THIS DEBUGGING. THIS LOGIC WILL HAVE BEEN TESTED IN ISOLATION, BUT NOT IN THIS LARGER REVIEW ROUTINE.

            var request = new ScanRequest(NationalGalleryOfArtIndexer.TABLE_REVIEW);
            var dynamoDbClient = GalleryAwsCredentialsFactory.ProductionDbClient;
            ScanResponse response = null;
            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }
                response = dynamoDbClient.ScanAsync(request).Result;
                //foreach (var item in response.Items)
                Parallel.ForEach(response.Items, item => 
                {
                    var model = JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(item).ToJson());
                    var s3Client = GalleryAwsCredentialsFactory.S3Client;
                    try
                    {
                        MoveS3ImageForReview(s3Client, model);
                    }
                    catch (AggregateException ex)
                    {
                        if (!ex.Message.Contains("specified key does not exist"))
                        {
                            throw;
                        }
                    }
                    
                });
            } while (response.LastEvaluatedKey.Any());

        }

        private void MoveForReview(
            IAmazonDynamoDB dbClient,
            ElasticSearchClient elasticSearchClient,
            ClassificationModel model)
        {
            var json = JObject.FromObject(model, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
            
            var result = dbClient.PutItemAsync(
                NationalGalleryOfArtIndexer.TABLE_REVIEW,
                Document.FromJson(json.ToString()).ToAttributeMap()
            ).Result;
            
            if (result.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Failed to move to review table");
            }

            // HAZARD TEST THIS DEBUGGING. THIS LOGIC WILL HAVE BEEN TESTED IN ISOLATION, BUT NOT IN THIS LARGER REVIEW ROUTINE.
            var s3Client = GalleryAwsCredentialsFactory.S3Client;
            MoveS3ImageForReview(s3Client, model);

            try
            {
                var searchDeletionResult = elasticSearchClient.DeleteFromElasticSearch(model).Result;
                var searchDeletionResultJson = JObject.Parse(searchDeletionResult);
                if (!string.Equals(searchDeletionResultJson["result"].Value<string>(), "deleted", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Failed to delete from elastic search");
                }
            }
            catch (AggregateException exception)
            {
                if (!exception.Message.Contains("404 (Not Found)", StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }
            }

            var modelDeletionResult = dbClient.DeleteItemAsync(model.GetTable(), model.GetKey()).Result;
            if (modelDeletionResult.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Failed to delete from classification table");
            }
        }

        private void MoveS3ImageForReview(IAmazonS3 s3Client, ClassificationModel model)
        {
            var reviewImageCopyResult = s3Client.CopyObjectAsync(
                NationalGalleryOfArtIndexer.BUCKET,
                model.S3Path,
                NationalGalleryOfArtIndexer.BUCKET_REVIEW,
                model.S3Path
            ).Result;
            if (reviewImageCopyResult.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Failed to copy image to review bucket");
            }

            var imageOriginalDeleteResult = s3Client.DeleteObjectAsync(NationalGalleryOfArtIndexer.BUCKET, model.S3Path).Result;
            if (!string.Equals(imageOriginalDeleteResult.DeleteMarker, "true", StringComparison.OrdinalIgnoreCase)) // Assumes versioning is enabled. If versioning isn't enabled the the object shouldn't be deleted, because some of these images are no longer accessible and the operation would be hazardous.
            {
                throw new Exception("Failed to delete image from primary bucket");
            }
        }

        [Test]
        public void CreateQuery()
        {
            var filters = new List<string>();
            var source = "http://the-athenaeum.org";
            var searchText = "lawrence";
            if (!string.IsNullOrWhiteSpace(source))
            {
                filters.Add($@"
                  {{
                    ""term"": {{
                      ""source.keyword"": ""{source}""
                    }}
                  }}
                ");
            }
            var filter = $@"
            ,""filter"": {{
              ""bool"": {{
                ""must"": [
                  {string.Join(",", filters)}
                ]
              }}
            }}";
            var getRequest = $@"{{
              ""query"": {{
                ""bool"": {{
                  ""must"": {{
                    ""multi_match"": {{
                      ""query"": ""{searchText}"",
                      ""type"": ""best_fields"",
                      ""fields"": [
                        ""artist^2"",
                        ""name"",
                        ""date""
                      ]
                    }}
                  }}
                  { filter }
                }}
              }},
              ""size"": {200}
            }}";
            Console.WriteLine(getRequest);
        }

        [Test]
        public void DeployArtIndexerInEachRegion()
        {
            var environmentVariables = new Dictionary<string, string>
            {
                { "ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN", Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN") },
                { "ELASTICSEARCH_API_ENDPOINT_FOUNDATION", Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION") }
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
                var receiveRequest =
                    new ReceiveMessageRequest(
                        "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler-failure")
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

                var result = sqsClient.SendMessageBatchAsync("https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler", sendMessages).Result;
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

        [Test]
        public void TestIndex()
        {
            var client = new HttpClient();
            var indexer = new IndexerFactory().GetIndexer(NationalGalleryOfArtIndexer.Source, client, null, null);
            var result = indexer.Index("100515", null).Result;
            Console.WriteLine(result.Model.Source);
            Console.WriteLine(result.Model.PageId); 
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
    }
}
