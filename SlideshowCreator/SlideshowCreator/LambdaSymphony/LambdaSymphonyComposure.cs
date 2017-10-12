using System.IO;
using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace SlideshowCreator.LambdaSymphony
{
    class LambdaSymphonyComposure
    {
        public void CreateOrUpdateFunction(RegionEndpoint region)
        {
            var client = BackpageLambdaConfig.CreateLambdaClient(region);

            if (FunctionExists(BackpageLambdaConfig.AdIndexerFunctionName, region))
            {
                client.DeleteFunction(BackpageLambdaConfig.AdIndexerFunctionName);
            }

            var path = @"C:\Users\peon\Desktop\NestStatusCheck-f57d0018-9b64-4b07-a6ce-9ca438d8c526.zip";
            using (var fileMemoryStream = new MemoryStream(File.ReadAllBytes(path)))
            {
                var createFunctionRequest = new CreateFunctionRequest();
                createFunctionRequest.FunctionName = BackpageLambdaConfig.AdIndexerFunctionName;
                createFunctionRequest.Runtime = Runtime.Dotnetcore10;
                createFunctionRequest.Handler = "Nest::Nest.Function::FunctionHandler";
                createFunctionRequest.Role = "arn:aws:iam::363557355695:role/lambda_exec_Backpage";
                createFunctionRequest.Code = new FunctionCode { ZipFile = fileMemoryStream };

                client.CreateFunction(createFunctionRequest);
            }
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
