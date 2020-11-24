using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.SQS;
using Amazon.SQS.Model;
using GalleryBackend;
using IndexBackend.Indexing;
using IndexBackend.MinistereDeLaCulture;
using IndexBackend.MuseeOrsay;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Console = System.Console;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ArtIndexer
{
    public class Function
    {
        private IAmazonSQS QueueClient { get; }
        private IAmazonDynamoDB DbClient { get; }
        private HttpClient HttpClient { get; }
        private IAmazonS3 S3Client { get; }
        private const string QUEUE_URL = "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler";
        private const int SQS_MAX_BATCH = 10;

        public Function()
            : this(
                new AmazonSQSClient(new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USEast1 }),
                new AmazonDynamoDBClient(new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.USEast1}),
                new AmazonS3Client(),
                new HttpClient()
            )
        {

        }

        public Function(IAmazonSQS queueClient, IAmazonDynamoDB dbClient, IAmazonS3 s3Client, HttpClient httpClient)
        {
            QueueClient = queueClient;
            DbClient = dbClient;
            S3Client = s3Client;
            HttpClient = httpClient;
        }

        public string FunctionHandler(ILambdaContext context)
        {
            ReceiveMessageResponse batch;
            do
            {
                batch = QueueClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    MaxNumberOfMessages = SQS_MAX_BATCH,
                    QueueUrl = QUEUE_URL
                }).Result;
                if (!batch.Messages.Any())
                {
                    break;
                }
                var tasks = new List<Task>();
                foreach (var message in batch.Messages)
                {
                    tasks.Add(IndexAndMarkComplete(message));
                }
                Task.WaitAll(tasks.ToArray());
            } while (batch.Messages.Any());
            return $"No additional SQS messages found in {QUEUE_URL}";
        }

        private async Task IndexAndMarkComplete(Message message)
        {
            var messageJson = JObject.Parse(message.Body);
            var id = (messageJson["id"] ?? string.Empty).Value<string>();
            var source = (messageJson["source"] ?? string.Empty).Value<string>();
            var indexer = GetIndexer(source);
            if (indexer == null)
            {
                Console.WriteLine($"Failed to process message due to unknown source {source} for message: {messageJson.ToString(Formatting.None)}");
                return;
            }
            try
            {
                var result = await indexer.Index(id);
                if (result == null)
                {
                    Console.WriteLine($"Skipped {messageJson.ToString(Formatting.None)} due to not finding content.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to process message due to error {e.Message} for message: {messageJson.ToString(Formatting.None)}. Error: " + e);
                return;
            }
            await QueueClient.DeleteMessageAsync(QUEUE_URL, message.ReceiptHandle);
        }

        private IIndex GetIndexer(string source)
        {
            if (string.Equals(source, MuseeOrsayIndexer.Source, StringComparison.OrdinalIgnoreCase))
            {
                return new MuseeOrsayIndexer(DbClient, S3Client, HttpClient);
            }
            if (string.Equals(source, MinistereDeLaCultureIndexer.SourceMuseeDuLouvre) ||
                string.Equals(source, MinistereDeLaCultureIndexer.SourceMinistereDeLaCulture))
            {
                return new MinistereDeLaCultureIndexer(DbClient, S3Client, HttpClient, new ConsoleLogging(), source,
                    string.Equals(source, MinistereDeLaCultureIndexer.SourceMuseeDuLouvre)
                        ? MinistereDeLaCultureIndexer.S3PathLouvre // Not a big deal if new louvre images go into the parent folder, but the existing louvre images are in the louvre folder.
                        : MinistereDeLaCultureIndexer.S3PathMinistereDeLaCulture);
            }
            return null;
        }

    }
}
