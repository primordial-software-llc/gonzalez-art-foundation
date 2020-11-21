using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.SQS;
using Amazon.SQS.Model;
using IndexBackend.MuseeOrsay;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ArtIndexer
{
    public class Function
    {
        private IAmazonSQS QueueClient { get; }
        private IAmazonDynamoDB DbClient { get; }
        private IAmazonS3 S3Client { get; }
        private const string QUEUE_URL = "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler";
        private const int SQS_MAX_BATCH = 10;

        public Function()
            : this(
                new AmazonSQSClient(new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USEast1 }),
                new AmazonDynamoDBClient(new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.USEast1}),
                new AmazonS3Client()
            )
        {

        }

        public Function(IAmazonSQS queueClient, IAmazonDynamoDB dbClient, IAmazonS3 s3Client)
        {
            QueueClient = queueClient;
            DbClient = dbClient;
            S3Client = s3Client;
        }

        public string FunctionHandler(ILambdaContext context)
        {
            ReceiveMessageResponse sectionBatch;
            do
            {
                sectionBatch = QueueClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    MaxNumberOfMessages = SQS_MAX_BATCH,
                    QueueUrl = QUEUE_URL
                }).Result;
                if (!sectionBatch.Messages.Any())
                {
                    break;
                }
                Console.WriteLine($"Processing {sectionBatch.Messages.Count} messages in an SQS batch.");
                var indexer = new MuseeOrsayIndexer(DbClient, S3Client);
                SemaphoreSlim maxThread = new SemaphoreSlim(SQS_MAX_BATCH, SQS_MAX_BATCH);
                var tasks = new ConcurrentDictionary<int, Task>();

                foreach (var message in sectionBatch.Messages)
                {
                    var messageJson = JObject.Parse(message.Body);
                    var id = messageJson["id"].Value<int>();
                    maxThread.Wait();
                    var added = tasks.TryAdd(
                        id,
                        indexer.Index(id)
                            .ContinueWith(task =>
                            {
                                maxThread.Release();
                                tasks.TryRemove(id, out Task removedTask); // I don't care if the task can't be removed it's just removed to prevent a memory issue with a large batch.
                                QueueClient.DeleteMessageAsync(QUEUE_URL, message.ReceiptHandle).Wait();
                            }));
                    if (!added)
                    {
                        throw new Exception("Failed to add task to concurrent dictionary"); // I do care if it's not removed, because then the message will be unnecessarily retried and could back up the queue.
                    }
                }
                Task.WaitAll(tasks.Select(x => x.Value).ToArray());
            } while (sectionBatch.Messages.Any());

            return $"No additional SQS messages found in {QUEUE_URL}";
        }

    }
}
