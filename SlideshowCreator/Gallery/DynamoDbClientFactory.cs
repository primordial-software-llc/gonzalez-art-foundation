using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using GalleryBackend.Classification;
using GalleryBackend.DataAccess;

namespace MVC5App
{
    public class DynamoDbClientFactory
    {
        private readonly AWSCredentials instanceCredentials = new InstanceProfileAWSCredentials();
        private readonly ImageClassificationAccess access;

        public DynamoDbClientFactory()
        {
            var client = new AmazonDynamoDBClient(instanceCredentials, RegionEndpoint.USEast1);
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
            return access.Scan(lastPageId);
        }

    }
}