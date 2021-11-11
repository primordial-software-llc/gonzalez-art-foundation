using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.S3;
using ArtApi.Model;
using IndexBackend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace DistributedProcessor
{
    public class Function
    {
        private IAmazonDynamoDB DbClient { get; }
        private IAmazonS3 S3Client { get; }
        private ElasticSearchClient ElasticSearchClient { get; }

        public Function()
            : this(
                new AmazonDynamoDBClient(new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.USEast1}),
                new AmazonS3Client(),
                new ElasticSearchClient(
                    new HttpClient(),
                    Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION"),
                    Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN"))
            )
        {

        }

        public Function(
            IAmazonDynamoDB dbClient,
            IAmazonS3 s3Client,
            ElasticSearchClient elasticSearchClient)
        {
            DbClient = dbClient;
            S3Client = s3Client;
            ElasticSearchClient = elasticSearchClient;
        }

        public string FunctionHandler(ILambdaContext context)
        {
            var request = new ScanRequest(new ClassificationModel().GetTable())
            {
                FilterExpression = "attribute_not_exists(orientation)"
            };
            ScanResponse response = null;
            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }
                response = DbClient.ScanAsync(request).Result;
                Parallel.ForEach(response.Items, new ParallelOptions { MaxDegreeOfParallelism = 2 }, item =>
                {
                    var modelJson = Document.FromAttributeMap(item).ToJson();
                    var classification = JsonConvert.DeserializeObject<ClassificationModel>(modelJson);
                    try
                    {
                        Process(classification).Wait();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to re-process source: {classification.Source} page id: {classification.PageId}: {e}");
                    }
                });
            } while (response.LastEvaluatedKey.Any());
            return "Finished re-processing all records without orientation.";
        }

        private async Task Process(ClassificationModel classification)
        {
            Console.WriteLine($"Re-processing source: {classification.Source} page id: {classification.PageId}");
            if (classification.ModerationLabels != null && !classification.Nudity.HasValue)
            {
                classification.Nudity = classification.ModerationLabels.Any(x =>
                    x.Name.Contains("nudity", StringComparison.OrdinalIgnoreCase) ||
                    x.ParentName.Contains("nudity", StringComparison.OrdinalIgnoreCase)
                );
            }
            classification.S3Bucket = Constants.IMAGES_BUCKET;
            var objectImage = S3Client.GetObjectAsync(Constants.IMAGES_BUCKET, $"{classification.S3Path}").Result;

            if (objectImage.ContentLength == 0)
            {
                new ReviewProcess().MoveForReview(DbClient, ElasticSearchClient, S3Client, classification);
                return;
            }

            byte[] imageBytes;
            await using (var stream = objectImage.ResponseStream)
            await using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }
            using var image = Image.Load(imageBytes);

            classification.Height = image.Height;
            classification.Width = image.Width;
            classification.Orientation = image.Height >= image.Width ? Constants.ORIENTATION_PORTRAIT : Constants.ORIENTATION_LANDSCAPE;

            var json = JObject.FromObject(classification, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
            await ElasticSearchClient.SendToElasticSearch(classification);
            await DbClient.PutItemAsync(
                new ClassificationModel().GetTable(),
                Document.FromJson(json.ToString()).ToAttributeMap()
            );
        }

    }
}
