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
using Amazon.S3.Model;
using ArtApi.Model;
using IndexBackend;
using IndexBackend.DataMaintenance;
using IndexBackend.Indexing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.Processing;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ThumbnailGenerator
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

        // Need an index. This is now timing out and exceeding dynamodb capacity.
        public string FunctionHandler(ILambdaContext context)
        {
            var request = new ScanRequest(new ClassificationModel().GetTable())
            {
                FilterExpression = "attribute_not_exists(s3ThumbnailPath)"
            };
            ScanResponse response = null;
            do
            {
                if (response != null)
                {
                    request.ExclusiveStartKey = response.LastEvaluatedKey;
                }
                response = DbClient.ScanAsync(request).Result;
                foreach (var item in response.Items)
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
                }
            } while (response.LastEvaluatedKey.Any());
            return "Finished re-processing all records without orientation.";
        }

        private async Task Process(ClassificationModel classification)
        {
            var objectImage = S3Client.GetObjectAsync(Constants.IMAGES_BUCKET, $"{classification.S3Path}").Result;
            if (objectImage.ContentLength == 0)
            {
                new ReviewAndArchiveProcess().MoveForReview(DbClient, ElasticSearchClient, S3Client, classification);
                return;
            }

            await using var imageStream = objectImage.ResponseStream;
            await using var imageMemoryStream = new MemoryStream();
            await imageStream.CopyToAsync(imageMemoryStream);

            var thumbnailBytes = ImageCompression.CreateThumbnail(imageMemoryStream.ToArray(), ImageCompression.DefaultSize, KnownResamplers.Lanczos3);
            await using var thumbnailStream = new MemoryStream(thumbnailBytes);
            var s3FileName = classification.S3Path.Split("/").Last();
            var s3PurePath = classification.S3Path.Replace("/" + s3FileName, string.Empty);
            var createThumbnailRequest = new PutObjectRequest
            {
                BucketName = $"{Constants.IMAGES_BUCKET}/{s3PurePath}/thumbnails",
                Key = s3FileName,
                InputStream = thumbnailStream
            };
            classification.S3ThumbnailPath = $"{s3PurePath}/thumbnails/{s3FileName}";

            await S3Client.PutObjectAsync(createThumbnailRequest);
            var json = JObject.FromObject(classification, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
            await ElasticSearchClient.SendToElasticSearch(classification);
            await DbClient.PutItemAsync(
                new ClassificationModel().GetTable(),
                Document.FromJson(json.ToString()).ToAttributeMap()
            );
        }

    }
}
