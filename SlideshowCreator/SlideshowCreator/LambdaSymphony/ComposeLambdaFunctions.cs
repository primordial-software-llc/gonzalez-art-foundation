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
                var client = BackpageLambdaConfig.CreateLambdaClient(region);
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
        public void Rebuild_Lamda_Functions_In_Each_Region()
        {
            foreach (var region in BackpageLambdaConfig.Regions)
            {
                new LambdaSymphonyComposure().CreateOrUpdateFunction(region);
                Assert.IsTrue(new LambdaSymphonyComposure().FunctionExists(BackpageLambdaConfig.AdIndexerFunctionName, region));
            }
        }

        [Test]
        public void Test_Non_Existing_Function()
        {
            Assert.IsFalse(new LambdaSymphonyComposure().FunctionExists(Guid.NewGuid().ToString(), RegionEndpoint.USEast1));
        }

        [Test]
        public void Test_Existing_Function()
        {
            Assert.IsTrue(new LambdaSymphonyComposure().FunctionExists("NestStatusCheck", RegionEndpoint.USEast1));
        }

    }
}
