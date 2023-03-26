using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.S3.Model;
using ArtApi.Model;
using IndexBackend;
using IndexBackend.DataMaintenance;
using IndexBackend.Sources.MuseeOrsay;
using Newtonsoft.Json;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Document = Amazon.DynamoDBv2.DocumentModel.Document;

namespace SlideshowCreator.Scripts
{
    class AdministrationScripts
    {

        [Test]
        public void RebuildArtistList()
        {
            var rebuildArtistListTableProcess = new RebuildArtistListTableProcess(GalleryAwsCredentialsFactory.ProductionDbClient);
            rebuildArtistListTableProcess.RebuildArtistListTable(Constants.IMAGES_TABLE, Constants.ARTIST_TABLE);
        }

        // Needs some work if you want it to run in parallel with other processing.
        // Need to do patches instead of updates on all fields.
        // No real need right now.
        // I can go as fast as I can afford.
        [Test]
        public void SetImagesWithoutOrientation()
        {
            var request = new ScanRequest(new ClassificationModel().GetTable())
            {
                FilterExpression = "attribute_not_exists(orientation) or attribute_not_exists(height) or attribute_not_exists(width)"
            };
            var dbClient = new DatabaseClient<ClassificationModel>(GalleryAwsCredentialsFactory.ProductionDbClient);
            var response = dbClient.ScanAll(request);
            var s3Client = GalleryAwsCredentialsFactory.S3Client;
            var elasticSearchClient = GalleryAwsCredentialsFactory.ElasticSearchClient;
            foreach (var item in response)
            {
                using var imageStream = new MemoryStream();
                using var objectImage = s3Client.GetObjectAsync(Constants.IMAGES_BUCKET, item.S3Path).Result;
                using var responseStream = objectImage.ResponseStream;
                using var reader = new StreamReader(responseStream);
                using var memoryStream = new MemoryStream();
                responseStream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();
                var imageJpeg = Image.Load<Rgba64>(bytes);
                item.Height = imageJpeg.Height;
                item.Width = imageJpeg.Width;
                item.Orientation = imageJpeg.Height >= imageJpeg.Width
                    ? Constants.ORIENTATION_PORTRAIT
                    : Constants.ORIENTATION_LANDSCAPE;
                Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
                dbClient.Update(item);
                elasticSearchClient.SendToElasticSearch(item).Wait();
            }
        }

        //[Test] WARNING - Don't run this until the date is dynamic. It's 1924 when the date is 2019 or 95 years in the past.
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
                    int? parsedYear = ImageDateParsing.ParseDate(model.Date);
                    if (!parsedYear.HasValue)
                    {
                        Console.WriteLine("Unknown date: " + model.Date);
                    }
                    else if (parsedYear > thresholdYear)
                    {
                        new ReviewAndArchiveProcess().MoveForReview(dynamoDbClient, elasticSearchClient, s3Client, model);
                        Thread.Sleep(200);
                    }
                }
            } while (response.LastEvaluatedKey.Any());
        }
        
        // Might want to purge this.
        [Test]
        public void ArchiveImageSource()
        {
            var request = new QueryRequest(new ClassificationModel().GetTable())
            {
                ScanIndexForward = true,
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":source", new AttributeValue { S = Constants.SOURCE_MUSEE_DORSAY } }
                },
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#source", "source" }
                },
                KeyConditionExpression = "#source = :source",
                Limit = 100
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
                Parallel.ForEach(response.Items, new ParallelOptions { MaxDegreeOfParallelism = 20 }, item =>
                {
                    var model = JsonConvert.DeserializeObject<ClassificationModel>(Document.FromAttributeMap(item)
                        .ToJson());
                    var sourceBucket = model.S3Bucket;
                    if (string.IsNullOrWhiteSpace(sourceBucket))
                    {
                        sourceBucket = Constants.IMAGES_BUCKET;
                    }

                    new ReviewAndArchiveProcess().MoveForArchive(
                        dynamoDbClient,
                        elasticSearchClient,
                        s3Client,
                        model,
                        model.GetKey(),
                        model.S3Path,
                        model.S3ThumbnailPath,
                        model.GetTable(),
                        sourceBucket);
                });
            } while (response.LastEvaluatedKey.Any());
        }

        // HAZARD: ONLY USE THIS IF YOU ARE ABSOLUTELY CERTAIN
        // THAT YOU CAN DELETE THE IMAGES. THIS BYPASSES VERSIONING AS IT DELETES ALL VERSIONS.
        // WHEN PROCESSES LIKE COMPLIANCE REVIEW GET MORE MATURE S3 VERSIONS WILL BE IMMUTABLE.
        //[Test]
        public void DeleteS3Versions()
        {
            var s3Client = GalleryAwsCredentialsFactory.S3Client;
            ListVersionsResponse versionResponse;
            do
            {
                versionResponse = s3Client.ListVersionsAsync(new ListVersionsRequest
                {
                    BucketName = Constants.IMAGES_BUCKET,
                    Prefix = new MuseeOrsayIndexer(null, null).ImagePath
                }).Result;
                Parallel.ForEach(versionResponse.Versions, version =>
                {
                    var deleteResult = s3Client.DeleteObjectAsync(new DeleteObjectRequest
                    {
                        BucketName = Constants.IMAGES_BUCKET,
                        Key = version.Key,
                        VersionId = version.VersionId
                    }).Result;
                });
            } while (versionResponse.Versions.Any());
        }
    }
}
