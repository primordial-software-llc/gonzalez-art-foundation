using Amazon.DynamoDBv2;
using AwsTools;
using GalleryBackend.Model;

namespace IndexBackend.DataAccess
{
    public class GalleryUserAccess
    {
        private IAmazonDynamoDB Client { get; }
        private ILogging Logging { get; }

        public GalleryUserAccess(IAmazonDynamoDB client, ILogging logging)
        {
            Client = client;
            Logging = logging;
        }

        public GalleryUser GetUser()
        {
            var user = new GalleryUser {Id = "47dfa78b-9c28-41a5-9048-1df383e4c48a"};
            var awsToolsClient = new DynamoDbClient<GalleryUser>(Client, Logging);
            user = awsToolsClient.Get(user);
            return user;
        }
    }
}
