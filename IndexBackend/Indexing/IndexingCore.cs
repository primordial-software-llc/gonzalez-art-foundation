using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Rekognition;
using Amazon.S3;
using Amazon.S3.Model;
using ArtApi.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;

namespace IndexBackend.Indexing
{
    public class IndexingCore
    {
        private IAmazonDynamoDB DbClient { get; }
        private IAmazonS3 S3Client { get; }
        private ElasticSearchClient ElasticSearchClient { get; }

        private IAmazonRekognition AmazonRekognitionClient { get; }

        public IndexingCore(
            IAmazonDynamoDB dbClient,
            IAmazonS3 s3Client,
            ElasticSearchClient elasticSearchClient,
            IAmazonRekognition amazonRekognitionClient)
        {
            DbClient = dbClient;
            S3Client = s3Client;
            ElasticSearchClient = elasticSearchClient;
            AmazonRekognitionClient = amazonRekognitionClient;
        }

        public async Task Index(IIndex indexer, ClassificationModel messageModel)
        {
            var dbClient = new DatabaseClient<ClassificationModel>(DbClient);
            var existing = dbClient.Get(new ClassificationModel { Source = messageModel.Source, PageId = messageModel.PageId });
            if (existing != null)
            {
                throw new ProtectedClassificationException(
                    $"This record has already been crawled and is now protected: {messageModel.Source} - {messageModel.PageId}." +
                    " If you want to re-crawl the record delete it in dynamodb, but all associated data will be overwritten when re-crawled.");
            }
            var indexResult = await indexer.Index(messageModel.PageId);
             if (indexResult == null || indexResult.Model == null || indexResult.ImageJpegBytes == null)
            {
                Console.WriteLine($"Skipped {messageModel.Source} {messageModel.PageId} due to not finding content.");
            }
            else
            {
                var classification = indexResult.Model;
                await using var imageStream = new MemoryStream(indexResult.ImageJpegBytes);
                var request = new PutObjectRequest
                {
                    BucketName = Constants.IMAGES_BUCKET + "/" + indexer.ImagePath,
                    Key = $"page-id-{indexResult.Model.PageId}.jpg",
                    InputStream = imageStream
                };
                await S3Client.PutObjectAsync(request);
                using var image = Image.Load(indexResult.ImageJpegBytes);
                classification.Height = image.Height;
                classification.Width = image.Width;
                classification.Orientation = image.Height >= image.Width
                    ? Constants.ORIENTATION_PORTRAIT
                    : Constants.ORIENTATION_LANDSCAPE;
                classification.S3Path = indexer.ImagePath + "/" + $"page-id-{indexResult.Model.PageId}.jpg";
                classification.Name = HttpUtility.HtmlDecode(classification.Name);
                classification.Date = HttpUtility.HtmlDecode(classification.Date);
                classification.OriginalArtist = HttpUtility.HtmlDecode(classification.OriginalArtist);
                classification.Artist = Classifier.NormalizeArtist(HttpUtility.HtmlDecode(classification.OriginalArtist));
                classification.TimeStamp = DateTime.UtcNow.ToString("O");
                /*
                WARNING:
                I can't afford this right now, but it's very important.
                I'm not sure what I will do.
                I may have to analyze a little bit at a time in a separate process.
                Or I may have to spend the little time I have and implement an open source option, but it means I can host less images.

                What I really need is for primordial software to make some end of year donations so the business nets $0 and pays no income tax,
                if the business is not yet netting $0 and has potential deductions. There's no point in brute forcing this thing.
                My time is too valuable in other aspects of this organization.

                classification.ModerationLabels = new ImageAnalysis().GetImageAnalysis(
                    AmazonRekognitionClient,
                    Constants.IMAGES_BUCKET,
                    classification.S3Path);
                classification.Nudity = classification.ModerationLabels.Any(x =>
                    x.Name.Contains("nudity", StringComparison.OrdinalIgnoreCase) ||
                    x.ParentName.Contains("nudity", StringComparison.OrdinalIgnoreCase)
                );
                */
                classification.Nudity = false;
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
            }
        }
    }
}
