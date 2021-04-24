using System.IO;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.S3;
using IndexBackend.Indexing;
using IndexBackend.Model;
using IndexBackend.Sources.NationalGalleryOfArt;

namespace IndexBackend.Sources.TheAthenaeum
{
    public class TheAthenaeumIndexer : IIndex
    {
        public static string Source => "http://www.the-athenaeum.org";
        public string ImagePath => "collections/the-athenaeum";
        protected IAmazonS3 S3Client { get; }
        protected IAmazonDynamoDB DynamoDbClient { get; }

        public TheAthenaeumIndexer(IAmazonS3 s3Client, IAmazonDynamoDB dynamoDbClient)
        {
            S3Client = s3Client;
            DynamoDbClient = dynamoDbClient;
        }

        public Task<IndexResult> Index(string id, ClassificationModel existing)
        {
            if (existing.ModerationLabels != null)
            {
                return Task.FromResult(new IndexResult
                {
                    Model = existing
                });
            }
            var objectImage = S3Client.GetObjectAsync(NationalGalleryOfArtIndexer.BUCKET, existing.S3Path).Result;
            byte[] bytes;
            using (var stream = objectImage.ResponseStream)
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }
            var result = new IndexResult
            {
                Model = existing,
                ImageBytes = bytes
            };
            return Task.FromResult(result);
        }
    }
    
}
