using System;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using MVC5App.DataAccess;

namespace MVC5App
{
    public class DynamoDbClientFactory
    {

        public string SearchByExactArtist(string artistName)
        {
            AWSCredentials instanceCredentials = new InstanceProfileAWSCredentials();
            var client = new AmazonDynamoDBClient(instanceCredentials, RegionEndpoint.USEast1);
            var access = new ImageClassificationAccess(client);

            try
            {
                return access.FindAllForExactArtist(artistName.ToLower()).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public string SearchByLikeArtist(string artistName)
        {
            AWSCredentials instanceCredentials = new InstanceProfileAWSCredentials();
            var client = new AmazonDynamoDBClient(instanceCredentials, RegionEndpoint.USEast1);
            var access = new ImageClassificationAccess(client);

            try
            {
                return access.FindAllForLikeArtist(artistName.ToLower()).ToString();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

    }
}