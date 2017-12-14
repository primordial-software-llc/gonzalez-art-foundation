using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.DataAccess;

namespace MVC5App
{
    public class DynamoDbClientFactory
    {
        private readonly ImageClassificationAccess access;

        public static IAmazonDynamoDB Client => new AmazonDynamoDBClient(
            GalleryAwsCredentialsFactory.GetCredentialsForWebsite(),
            RegionEndpoint.USEast1);

        public DynamoDbClientFactory()
        {
            access = new ImageClassificationAccess(Client);
        }

        public List<ClassificationModel> SearchByExactArtist(string artistName, string source)
        {
            return access.FindAllForExactArtist(artistName, source);
        }

        public List<ClassificationModel> SearchByLikeArtist(string artistName, string source)
        {
            return access.FindAllForLikeArtist(artistName, source);
        }

        public List<ClassificationModel> ScanByPage(int? lastPageId, string source)
        {
            return access.Scan(lastPageId, source);
        }

        public List<ImageLabel> SearchByLabel(string label, string source)
        {
            return access.FindByLabel(label, source);
        }

        public ImageLabel GetLabel(int pageId)
        {
            return access.GetLabel(pageId);
        }
        
    }
}