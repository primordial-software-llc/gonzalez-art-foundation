using System.IO;
using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace SlideshowCreator.LambdaSymphony
{
    class LambdaSymphonyComposure
    {
        public CreateFunctionRequest RebuildFunction(RegionEndpoint region, string deploymentPackagePath)
        {
            var client = BackpageLambdaConfig.CreateLambdaClient(region);

            if (FunctionExists(BackpageLambdaConfig.AdIndexerFunctionName, region))
            {
                client.DeleteFunction(BackpageLambdaConfig.AdIndexerFunctionName);
            }

            var createFunctionRequest = new CreateFunctionRequest();
            using (var fileMemoryStream = new MemoryStream(File.ReadAllBytes(deploymentPackagePath)))
            {
                createFunctionRequest.FunctionName = BackpageLambdaConfig.AdIndexerFunctionName;
                createFunctionRequest.Runtime = Runtime.Dotnetcore10;
                createFunctionRequest.Handler = "Nest::Nest.Function::FunctionHandler";
                createFunctionRequest.Role = "arn:aws:iam::363557355695:role/lambda_exec_Backpage";
                createFunctionRequest.Code = new FunctionCode { ZipFile = fileMemoryStream };

                client.CreateFunction(createFunctionRequest);
            }

            return createFunctionRequest;
        }

        public bool FunctionExists(string functionName, RegionEndpoint region)
        {
            var client = BackpageLambdaConfig.CreateLambdaClient(region);
            // Can't filter results and they are paged.
            try
            {
                client.GetFunction(functionName);
                return true;
            }
            catch (ResourceNotFoundException)
            {
                return false;
            }
        }

    }
}
