using System;
using System.Net.Http;
using IndexBackend;
using NUnit.Framework;

namespace SlideshowCreator.Tests
{
    public class DistributedProcessorTests
    {
        [Test]
        public void TestDistributedProcessorLocally()
        {
            var client = new HttpClient();
            var elasticClient = new ElasticSearchClient(
                client,
                Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION"),
                Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN"));
            var processor = new DistributedProcessor.Function(
                GalleryAwsCredentialsFactory.ProductionDbClient,
                GalleryAwsCredentialsFactory.S3Client,
                elasticClient
            );
            processor.FunctionHandler(null);
        }
    }
}
