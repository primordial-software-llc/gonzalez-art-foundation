﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using Amazon.S3.Model;
using ArtApi.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Processing;

namespace IndexBackend.Indexing
{
    public class IndexingCore
    {
        private IAmazonDynamoDB DbClient { get; }
        private IAmazonS3 S3Client { get; }
        private ElasticSearchClient ElasticSearchClient { get; }

        static IndexingCore()
        {
            Configuration.Default.MemoryAllocator = MemoryAllocator.Create(new MemoryAllocatorOptions
            {
                MaximumPoolSizeMegabytes = 64 // https://docs.sixlabors.com/articles/imagesharp/memorymanagement.html
            });
        }

        public IndexingCore(
            IAmazonDynamoDB dbClient,
            IAmazonS3 s3Client,
            ElasticSearchClient elasticSearchClient)
        {
            DbClient = dbClient;
            S3Client = s3Client;
            ElasticSearchClient = elasticSearchClient;
        }

        public async Task Index(IIndex indexer, ClassificationModel messageModel)
        {
            var dbClient = new DatabaseClient<ClassificationModel>(DbClient);
            var existing = dbClient.Get(new ClassificationModel { Source = messageModel.Source, PageId = messageModel.PageId });
            var activelyCrawledSources = new List<string>();
            if (existing != null && !activelyCrawledSources.Any(x => string.Equals(existing.Source, x, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ProtectedClassificationException(
                    $"This record has already been crawled and is now protected: {messageModel.Source} - {messageModel.PageId}." +
                    " If you want to re-crawl the record delete it in dynamodb, but all associated data will be overwritten when re-crawled.");
            }
            var indexResult = await indexer.Index(messageModel.PageId, existing);
            if (indexResult == null ||
                indexResult.Model == null ||
                indexResult.ImageJpeg == null)
            {
                var missingContent = new List<string>();
                if (indexResult?.Model == null)
                {
                    missingContent.Add("metadata");
                }
                if (indexResult?.ImageJpeg == null)
                {
                    missingContent.Add("binary");
                }
                throw new NoIndexContentException($"The following items are missing for the image {messageModel.Source} - {messageModel.PageId}: {string.Join(", ", missingContent)}");
            }
            else
            {
                var classification = indexResult.Model;
                if (indexResult.ImageJpeg != null)
                {
                    await using var imageStream = new MemoryStream();
                    await indexResult.ImageJpeg.SaveAsJpegAsync(imageStream);
                    await S3Client.PutObjectAsync(
                        new PutObjectRequest
                        {
                            BucketName = Constants.IMAGES_BUCKET + "/" + indexer.ImagePath,
                            Key = $"page-id-{indexResult.Model.PageId}.jpg",
                            InputStream = imageStream
                        });
                    classification.Height = indexResult.ImageJpeg.Height;
                    classification.Width = indexResult.ImageJpeg.Width;
                    classification.Orientation = indexResult.ImageJpeg.Height >= indexResult.ImageJpeg.Width
                        ? Constants.ORIENTATION_PORTRAIT
                        : Constants.ORIENTATION_LANDSCAPE;
                    var thumbnailBytes = ImageCompression.CreateThumbnail(imageStream.ToArray(), new Size(200, 200), KnownResamplers.Lanczos3);
                    await using var thumbnailStream = new MemoryStream(thumbnailBytes);
                    await S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = $"{Constants.IMAGES_BUCKET}/{indexer.ImagePath}/thumbnails",
                        Key = $"page-id-{indexResult.Model.PageId}.jpg",
                        InputStream = thumbnailStream
                    });
                    indexResult.ImageJpeg.Dispose();
                }
                else
                {
                    classification.Height = existing.Height;
                    classification.Width = existing.Width;
                    classification.Orientation = existing.Orientation;
                }
                classification.Name = HttpUtility.HtmlDecode(classification.Name);
                classification.Date = HttpUtility.HtmlDecode(classification.Date);
                classification.OriginalArtist = HttpUtility.HtmlDecode(classification.OriginalArtist);
                classification.Artist = Classifier.NormalizeArtist(HttpUtility.HtmlDecode(classification.OriginalArtist));
                classification.TimeStamp = DateTime.UtcNow.ToString("O");
                classification.Nudity = false;
                classification.S3Path = indexer.ImagePath + "/" + $"page-id-{indexResult.Model.PageId}.jpg";
                classification.S3ThumbnailPath = indexer.ImagePath + "/thumbnails/" + $"page-id-{indexResult.Model.PageId}.jpg";
                classification.S3Bucket = Constants.IMAGES_BUCKET;
                var json = JObject.FromObject(classification,
                    new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                await ElasticSearchClient.SendToElasticSearch(classification);
                var artistClient = new DatabaseClient<ArtistModel>(DbClient);
                var newArtistRecord = new ArtistModel { Artist = classification.Artist, OriginalArtist = classification.OriginalArtist };
                var artistRecord = artistClient.Get(newArtistRecord);
                if (artistRecord == null)
                {
                    artistClient.Create(newArtistRecord);
                }
                await DbClient.PutItemAsync(
                    new ClassificationModel().GetTable(),
                    Document.FromJson(json.ToString()).ToAttributeMap()
                );
                Console.WriteLine($"Indexed: { messageModel.Source} { messageModel.PageId}");
            }
        }
    }
}
