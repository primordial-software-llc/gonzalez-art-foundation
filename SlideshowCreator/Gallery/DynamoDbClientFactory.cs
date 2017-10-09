using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2;
using GalleryBackend.Model;
using IndexBackend;
using IndexBackend.DataAccess;
using IndexBackend.Indexing;

namespace MVC5App
{
    public class DynamoDbClientFactory
    {
        private readonly ImageClassificationAccess access;
        public DynamoDbClientFactory()
        {
            var client = new AmazonDynamoDBClient(
                GalleryAwsCredentialsFactory.GetCredentialsForWebsite(),
                RegionEndpoint.USEast1);
            access = new ImageClassificationAccess(client);
        }

        public List<ClassificationModel> SearchByExactArtist(string artistName)
        {
            return access.FindAllForExactArtist(artistName);
        }

        public List<ClassificationModel> SearchByLikeArtist(string artistName)
        {
            return access.FindAllForLikeArtist(artistName);
        }

        public List<ClassificationModel> ScanByPage(int lastPageId)
        {
            return access.Scan(lastPageId, new TheAthenaeumIndexer().Source);
        }

    }
}