using System;
using System.Net.Http;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Rekognition;
using Amazon.S3;
using Amazon.SQS;
using IndexBackend;
using IndexBackend.Indexing;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ArtIndexer
{
    public class Function
    {
        private IAmazonSQS QueueClient { get; }
        private IndexingCore IndexingCore { get; }
        private HttpClient HttpClient { get; }

        public Function()
            : this(
                new AmazonSQSClient(new AmazonSQSConfig { RegionEndpoint = RegionEndpoint.USEast1 }),
                new HttpClient(),
                new IndexingCore(
                    new AmazonDynamoDBClient(new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.USEast1 }),
                    new AmazonS3Client(),
                    new ElasticSearchClient(
                        new HttpClient(),
                        Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION"),
                        Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN")),
                    new AmazonRekognitionClient()
                ))
        {

        }

        public Function(IAmazonSQS queueClient, HttpClient httpClient, IndexingCore indexingCore)
        {
            QueueClient = queueClient;
            IndexingCore = indexingCore;
            HttpClient = httpClient;
        }

        public string FunctionHandler(ILambdaContext context)
        {
            var queueIndexer = new QueueIndexer(QueueClient, HttpClient, IndexingCore, new ConsoleLogging());
            return queueIndexer.ProcessAllInQueue();
        }

    }
}
