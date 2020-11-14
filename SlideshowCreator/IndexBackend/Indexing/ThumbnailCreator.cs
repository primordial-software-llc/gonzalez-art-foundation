using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using GalleryBackend.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IndexBackend.Indexing
{
    public class ThumbnailCreator
    {
        /// <summary>
        /// HAZARD: Deletes database record if the image isn't found. In this case the page needs to be re-crawled.
        /// I should probably remove this once the old site has been rebuilt.
        /// </summary>
        public async Task CreateThumbnail(
            IAmazonDynamoDB dbClient, 
            IAmazonS3 s3Client,
            string s3Bucket,
            string thumbnailPath,
            ClassificationModelNew itemParsed,
            bool deleteRecordIfS3FileDoesntExist)
        {
            GetObjectResponse s3File;

            try
            {
                s3File = await s3Client.GetObjectAsync(s3Bucket, itemParsed.S3Path);
            }
            catch (Exception e)
            {
                if (deleteRecordIfS3FileDoesntExist && e.Message == "The specified key does not exist.")
                {
                    var deleted = await dbClient.DeleteItemAsync(itemParsed.GetTable(), itemParsed.GetKey());
                    return;
                }
                else
                {
                    throw;
                }
            }

            using (var stream = s3File.ResponseStream)
            {
                var image = System.Drawing.Image.FromStream(stream);
                var thumbnailSize = ResizeKeepAspect(image.Size, 200, 200);
                var thumbnail = image.GetThumbnailImage(
                    thumbnailSize.Width,
                    thumbnailSize.Height,
                    () => false,
                    IntPtr.Zero);
                var path = $"C:\\Users\\peon\\Desktop\\thumbnails\\{Guid.NewGuid()}.jpg";
                thumbnail.Save(path);
                itemParsed.S3ThumbnailPath = $"{thumbnailPath}page-id-{itemParsed.PageId}.jpg";
                using (var fs = File.OpenRead(path))
                {
                    await s3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = "gonzalez-art-foundation",
                        Key = itemParsed.S3ThumbnailPath,
                        InputStream = fs
                    });
                }
                Dictionary<string, AttributeValue> key = itemParsed.GetKey();
                var updateJson = JObject.FromObject(itemParsed, new JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                foreach (var keyPart in key.Keys)
                {
                    updateJson.Remove(keyPart);
                }
                var updates = Document.FromJson(updateJson.ToString()).ToAttributeUpdateMap(false);
                await dbClient.UpdateItemAsync(
                    itemParsed.GetTable(),
                    key,
                    updates);
            }
        }

        private static Size ResizeKeepAspect(Size src, int maxWidth, int maxHeight)
        {
            maxWidth = Math.Min(maxWidth, src.Width);
            maxHeight = Math.Min(maxHeight, src.Height);

            decimal rnd = Math.Min(maxWidth / (decimal)src.Width, maxHeight / (decimal)src.Height);
            return new Size((int)Math.Round(src.Width * rnd), (int)Math.Round(src.Height * rnd));
        }
    }
}
