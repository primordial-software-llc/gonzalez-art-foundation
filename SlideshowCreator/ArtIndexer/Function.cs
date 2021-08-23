using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Amazon.Rekognition;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using IndexBackend;
using IndexBackend.Indexing;
using IndexBackend.Model;
using IndexBackend.Sources.NationalGalleryOfArt;
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
        private ElasticSearchClient ElasticSearchClient { get; }
        private const string QUEUE_URL = "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler";
        private const int SQS_MAX_BATCH = 10;

        public Function()
            : this(
                new AmazonSQSClient(new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USEast1 }),
                new AmazonDynamoDBClient(new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.USEast1}),
                new AmazonS3Client(),
                new HttpClient(),
                new ElasticSearchClient(
                    new HttpClient(),
                    Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION"),
                    Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN"))
            )
        {

        }

        public Function(IAmazonSQS queueClient, IAmazonDynamoDB dbClient, IAmazonS3 s3Client, HttpClient httpClient,
            ElasticSearchClient elasticSearchClient)
        {
            QueueClient = queueClient;
            DbClient = dbClient;
            S3Client = s3Client;
            HttpClient = httpClient;
            ElasticSearchClient = elasticSearchClient;
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
            var model = JsonConvert.DeserializeObject<ClassificationModel>(message.Body);
            var indexer = new IndexerFactory().GetIndexer(model.Source, HttpClient, S3Client, DbClient);
            if (indexer == null)
            {
                Console.WriteLine($"Failed to process message due to unknown source {model.Source} for message: {message.Body}");
                return;
            }
            try
            {
                var dbClient = new DatabaseClient<ClassificationModel>(DbClient);
                var existing = dbClient.Get(new ClassificationModel { Source = model.Source, PageId = model.PageId });
                if (existing != null)
                {
                    Console.WriteLine(
                        $"This record has already been crawled and is being skipped, because the record is now archived and protected: {model.Source} - {model.PageId}."+
                        $" If you want to re-crawl the record delete it in dynamodb and s3.");
                    await QueueClient.DeleteMessageAsync(QUEUE_URL, message.ReceiptHandle);
                    return;
                }
                var analyzedImageAlreadyExists = existing != null && existing.ModerationLabels != null;
                var indexResult = await indexer.Index(model.PageId, existing);
                if (indexResult?.Model == null)
                {
                    Console.WriteLine($"Skipped {message.Body} due to not finding content.");
                }
                else
                {
                    var classification = indexResult.Model;
                    if (!analyzedImageAlreadyExists)
                    {
                        await using var imageStream = new MemoryStream(indexResult.ImageBytes);
                        var request = new PutObjectRequest
                        {
                            BucketName = NationalGalleryOfArtIndexer.BUCKET + "/" + indexer.ImagePath,
                            Key = $"page-id-{indexResult.Model.PageId}.jpg",
                            InputStream = imageStream
                        };
                        await S3Client.PutObjectAsync(request);
                    }
                    classification.S3Path = indexer.ImagePath + "/" + $"page-id-{indexResult.Model.PageId}.jpg";
                    classification.Name = HttpUtility.HtmlDecode(classification.Name);
                    classification.Date = HttpUtility.HtmlDecode(classification.Date);
                    classification.OriginalArtist = HttpUtility.HtmlDecode(classification.OriginalArtist);
                    classification.Artist = Classifier.NormalizeArtist(HttpUtility.HtmlDecode(classification.OriginalArtist));
                    classification.TimeStamp = DateTime.UtcNow.ToString("O");
                    if (!analyzedImageAlreadyExists)
                    {
                        classification.ModerationLabels = new ImageAnalysis().GetImageAnalysis(new AmazonRekognitionClient(), NationalGalleryOfArtIndexer.BUCKET, classification.S3Path);
                    }
                    classification.Nudity = classification.ModerationLabels.Any(x =>
                        x.Name.Contains("nudity", StringComparison.OrdinalIgnoreCase) ||
                        x.ParentName.Contains("nudity", StringComparison.OrdinalIgnoreCase)
                    );
                    var json = JObject.FromObject(classification, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                    await DbClient.PutItemAsync(
                        new ClassificationModel().GetTable(),
                        Document.FromJson(json.ToString()).ToAttributeMap()
                    );
                    await ElasticSearchClient.SendToElasticSearch(classification);

                    var artistClient = new DatabaseClient<ArtistModel>(DbClient);
                    var newArtistRecord = new ArtistModel { Artist = model.Artist, OriginalArtist = model.OriginalArtist };
                    var artistRecord = artistClient.Get(newArtistRecord);
                    if (artistRecord == null)
                    {
                        artistClient.Create(newArtistRecord);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to process message due to error {e.Message} for message: {message.Body}. Error: " + e);
                return;
            }
            await QueueClient.DeleteMessageAsync(QUEUE_URL, message.ReceiptHandle);
        }

    }
}
