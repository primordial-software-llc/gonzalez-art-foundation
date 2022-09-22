using System;
using System.Collections.Generic;
using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using ArtApi.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IndexBackend.DataMaintenance
{
    public class ReviewAndArchiveProcess
    {
        public void MoveForReview(
            IAmazonDynamoDB dbClient,
            ElasticSearchClient elasticSearchClient,
            IAmazonS3 s3Client,
            ClassificationModel model)
        {
            MoveImage(
                dbClient,
                elasticSearchClient,
                s3Client,
                model,
                model.GetKey(),
                model.S3Path,
                model.S3Path,
                model.S3ThumbnailPath,
                model.GetTable(),
                Constants.IMAGES_TABLE_REVIEW,
                Constants.IMAGES_BUCKET,
                Constants.IMAGES_BUCKET_REVIEW,
                S3StorageClass.Standard);
        }

        public void MoveForArchive(
            IAmazonDynamoDB dbClient,
            ElasticSearchClient elasticSearchClient,
            IAmazonS3 s3Client,
            ClassificationModel model,
            Dictionary<string, AttributeValue> key,
            string sourceImagePath,
            string sourceThumbnailPath,
            string sourceTable,
            string sourceBucket)
        {
            MoveImage(
                dbClient,
                elasticSearchClient,
                s3Client,
                model,
                key,
                sourceImagePath,
                sourceImagePath,
                sourceThumbnailPath,
                sourceTable,
                Constants.IMAGES_TABLE_ARCHIVE,
                sourceBucket,
                Constants.IMAGES_BUCKET_ARCHIVE,
                S3StorageClass.DeepArchive);
        }

        /// <summary>
        /// CAUTION: Thumbnail is deleted when moving image, because it can easily be recreated.
        /// </summary>
        private void MoveImage(
            IAmazonDynamoDB dbClient,
            ElasticSearchClient elasticSearchClient,
            IAmazonS3 s3Client,
            ClassificationModel model,
            Dictionary<string, AttributeValue> key,
            string sourceImagePath,
            string targetImagePath,
            string sourceThumbnailPath,
            string sourceTable,
            string targetTable,
            string sourceBucket,
            string targetBucket,
            S3StorageClass targetStorageClass)
        {
            if (string.Equals(sourceTable, targetTable, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(sourceBucket, targetBucket, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Source table {sourceTable} and bucket {sourceBucket} must differ from the target table and bucket, but are the same.");
            }
            model.S3Bucket = targetBucket;
            model.S3ThumbnailPath = string.Empty;
            var json = JObject.FromObject(model, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
            var result = dbClient.PutItemAsync(
                targetTable,
                Document.FromJson(json.ToString()).ToAttributeMap()
            ).Result;

            if (result.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Failed to move to {targetTable} table");
            }

            MoveS3Image(
                s3Client,
                sourceImagePath,
                targetImagePath,
                sourceBucket,
                targetBucket,
                targetStorageClass);
            if (!string.IsNullOrWhiteSpace(sourceThumbnailPath))
            {
                var thumbnailDeleteResult = s3Client.DeleteObjectAsync(sourceBucket, sourceThumbnailPath).Result;
                if (!string.Equals(thumbnailDeleteResult.DeleteMarker, "true", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"Failed to delete {sourceBucket}/{sourceThumbnailPath}");
                }
            }

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

            var modelDeletionResult = dbClient.DeleteItemAsync(sourceTable, key).Result;
            if (modelDeletionResult.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"Failed to delete from {sourceTable} table");
            }
        }

        private void MoveS3Image(
            IAmazonS3 s3Client,
            string sourceImagePath,
            string targetImagePath,
            string sourceBucket,
            string targetBucket,
            S3StorageClass targetStorageClass)
        {
            try
            {
                var reviewImageCopyResult = s3Client.CopyObjectAsync(
                    new CopyObjectRequest
                    {
                        SourceBucket = sourceBucket,
                        SourceKey = sourceImagePath,
                        DestinationBucket = targetBucket,
                        DestinationKey = targetImagePath,
                        StorageClass = targetStorageClass
                    }).Result;

                if (reviewImageCopyResult.HttpStatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Failed to copy {sourceBucket}/{sourceImagePath} to {targetBucket}/{targetImagePath}");
                }

                var imageOriginalDeleteResult = s3Client.DeleteObjectAsync(sourceBucket, sourceImagePath).Result;
                if (!string.Equals(imageOriginalDeleteResult.DeleteMarker, "true", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception($"Failed to delete {sourceBucket}/{sourceImagePath}");
                }
            }
            catch (AggregateException e) when (e.InnerException != null && e.InnerException.Message.Contains("The specified key does not exist"))
            {
                Console.WriteLine($"Skipping image copy {sourceBucket}/{sourceImagePath} to {targetBucket}/{targetImagePath}");
            }

        }
    }
}
