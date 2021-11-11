using System;
using System.Net;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using ArtApi.Model;
using IndexBackend.Sources.NationalGalleryOfArt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IndexBackend
{
    public class ReviewProcess
    {
        public void MoveForReview(
            IAmazonDynamoDB dbClient,
            ElasticSearchClient elasticSearchClient,
            IAmazonS3 s3Client,
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
                Constants.IMAGES_BUCKET,
                model.S3Path,
                NationalGalleryOfArtIndexer.BUCKET_REVIEW,
                model.S3Path
            ).Result;
            if (reviewImageCopyResult.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Failed to copy image to review bucket");
            }

            var imageOriginalDeleteResult = s3Client.DeleteObjectAsync(Constants.IMAGES_BUCKET, model.S3Path).Result;
            if (!string.Equals(imageOriginalDeleteResult.DeleteMarker, "true", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Failed to delete image from primary bucket");
            }
        }
    }
}
