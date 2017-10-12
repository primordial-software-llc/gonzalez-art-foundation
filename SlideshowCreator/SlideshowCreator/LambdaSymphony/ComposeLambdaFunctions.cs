using System;
using System.IO;
using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using IndexBackend;
using NUnit.Framework;

namespace SlideshowCreator.LambdaSymphony
{
    class ComposeLambdaFunctions
    {
        [Test]
        public void Check_Lambda_Regions()
        {
            Assert.AreEqual(14, BackpageLambdaConfig.Regions.Count);
        }

        [Test]
        public void Print_Lamda_Functions_In_Each_Region()
        {
            foreach (var region in BackpageLambdaConfig.Regions)
            {
                var client = CreateLambdaClient(region);
                ListFunctionsResponse functionResponse = client.ListFunctions();
                Assert.IsNull(functionResponse.NextMarker);

                Console.WriteLine($"{functionResponse.Functions.Count} functions in {region.DisplayName}");
                foreach (var function in functionResponse.Functions)
                {
                    Console.WriteLine(function.FunctionName);
                }
            }
        }

        [Test]
        public void Create_Lamda_Functions_In_Each_Region()
        {
            var client = CreateLambdaClient(RegionEndpoint.USEast1);

            var path = @"C:\Users\peon\Desktop\NestStatusCheck-f57d0018-9b64-4b07-a6ce-9ca438d8c526.zip";
            using (var fileMemoryStream = new MemoryStream(File.ReadAllBytes(path)))
            {
                var createFunctionRequest = new CreateFunctionRequest();
                createFunctionRequest.FunctionName = BackpageLambdaConfig.AdIndexerFunctionName;
                createFunctionRequest.Runtime = Runtime.Dotnetcore10;
                createFunctionRequest.Handler = "Nest::Nest.Function::FunctionHandler";
                createFunctionRequest.Role = "arn:aws:iam::363557355695:role/lambda_exec_Backpage";
                createFunctionRequest.Code = new FunctionCode {ZipFile = fileMemoryStream};

                client.CreateFunction(createFunctionRequest);
            }


        }

        private AmazonLambdaClient CreateLambdaClient(RegionEndpoint region)
        {
            AmazonLambdaClient lambdaClient = new AmazonLambdaClient(
                GalleryAwsCredentialsFactory.CreateCredentialsFromDefaultProfile(),
                region);
            return lambdaClient;
        }

    }
}
