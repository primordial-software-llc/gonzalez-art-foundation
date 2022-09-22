using System;
using System.Collections.Generic;
using System.Net.Http;
using Amazon;
using Amazon.Lambda;
using AwsLambdaDeploy;
using IndexBackend;
using NUnit.Framework;

namespace SlideshowCreator.Tests
{
    public class ThumbnailGeneratorTests
    {

        [Test]
        public void Deploy()
        {
            var environmentVariables = new Dictionary<string, string>
            {
                { "ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN", Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN") },
                { "ELASTICSEARCH_API_ENDPOINT_FOUNDATION", Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION") }
            };

            var scheduledFrequencyInMinutes = 15;
            var increment = scheduledFrequencyInMinutes == 1 ? "minute" : "minutes";
            var scheduleExpression = $"rate({scheduledFrequencyInMinutes} {increment})";

            new LambdaDeploy().Deploy(
                GalleryAwsCredentialsFactory.CreateCredentials(),
                new List<RegionEndpoint>
                {
                    RegionEndpoint.USEast1
                },
                environmentVariables,
                scheduleExpression,
                "gonzalez-art-foundation-thumbnail-generator",
                @"C:\Users\peon\Desktop\projects\gonzalez-art-foundation-api\DistributedProcessing\ThumbnailGenerator.csproj",
                new LambdaEntrypointDefinition
                {
                    AssemblyName = "ThumbnailGenerator",
                    Namespace = "ThumbnailGenerator",
                    ClassName = "Function",
                    FunctionName = "FunctionHandler"
                },
                roleArn: "arn:aws:iam::283733643774:role/lambda_exec_art_api",
                runtime: Runtime.Dotnetcore31,
                8192,
                1,
                TimeSpan.FromMinutes(15));
        }

        [Test]
        public void TestDistributedProcessorLocally()
        {
            var client = new HttpClient();
            var elasticClient = new ElasticSearchClient(
                client,
                Environment.GetEnvironmentVariable("ELASTICSEARCH_API_ENDPOINT_FOUNDATION"),
                Environment.GetEnvironmentVariable("ELASTICSEARCH_API_KEY_GONZALEZ_ART_FOUNDATION_ADMIN"));
            var processor = new ThumbnailGenerator.Function(
                GalleryAwsCredentialsFactory.ProductionDbClient,
                GalleryAwsCredentialsFactory.S3Client,
                elasticClient
            );
            processor.FunctionHandler(null);
        }
        
    }
}
