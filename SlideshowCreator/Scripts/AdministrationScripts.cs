using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using ArtApi.Model;
using IndexBackend;
using IndexBackend.Sources.MinistereDeLaCulture;
using Newtonsoft.Json;
using NUnit.Framework;
using Document = Amazon.DynamoDBv2.DocumentModel.Document;

namespace SlideshowCreator.Scripts
{
    class AdministrationScripts
    {
        [Test]
        public void MoveSingleImageForReview()
        {
            var dynamoDbClient = GalleryAwsCredentialsFactory.ProductionDbClient;
            var modelResult = dynamoDbClient.GetItemAsync(new ClassificationModel().GetTable(),
                new ClassificationModel
                {
                    Source = MinistereDeLaCultureIndexer.SourceMinistereDeLaCulture,
                    PageId = "000PE024954"
                }.GetKey()).Result;
            var model = JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(modelResult.Item).ToJson());
            var s3Client = GalleryAwsCredentialsFactory.S3Client;
            new ReviewProcess().MoveForReview(dynamoDbClient, GalleryAwsCredentialsFactory.ElasticSearchClient, s3Client, model);
        }

        [Test]
        public void MoveClassificationsWithoutImageData()
        {
            var elasticSearchClient = GalleryAwsCredentialsFactory.ElasticSearchClient;
            var dynamoDbClient = GalleryAwsCredentialsFactory.ProductionDbClient;
            var s3Client = GalleryAwsCredentialsFactory.S3Client;
            var request = new ScanRequest(new ClassificationModel().GetTable())
            {
                FilterExpression = "attribute_not_exists(orientation)"
            };
            var response = dynamoDbClient.ScanAsync(request).Result;
            Parallel.ForEach(response.Items, new ParallelOptions { MaxDegreeOfParallelism = 10 }, item =>
            {
                var modelJson = Document.FromAttributeMap(item).ToJson();
                var classification = JsonConvert.DeserializeObject<ClassificationModel>(modelJson);
                var metaData = s3Client.GetObjectMetadataAsync(Constants.IMAGES_BUCKET, classification.S3Path).Result;
                if (metaData.ContentLength == 0)
                {
                    new ReviewProcess().MoveForReview(dynamoDbClient, elasticSearchClient, s3Client, classification);
                }
            });
        }

        [Test]
        public void MoveImagesNotInPublicDomain()
        {
            const int thresholdYear = 1924; // https://fairuse.stanford.edu/overview/public-domain/
            var request = new QueryRequest(new ClassificationModel().GetTable())
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":source", new AttributeValue { S = Constants.SOURCE_THE_ATHENAEUM } }
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#source", "source" }
                },
                KeyConditionExpression = "#source = :source"
            };
            var dynamoDbClient = GalleryAwsCredentialsFactory.ProductionDbClient;
            var elasticSearchClient = GalleryAwsCredentialsFactory.ElasticSearchClient;
            var s3Client = GalleryAwsCredentialsFactory.S3Client;
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
                        new ReviewProcess().MoveForReview(dynamoDbClient, elasticSearchClient, s3Client, model);
                        Thread.Sleep(200);
                    }
                }
            } while (response.LastEvaluatedKey.Any());
        }


    }
}
