using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;

namespace IndexBackend.LambdaSymphony
{
    public class LambdaDeploy
    {
        public List<Tuple<CreateFunctionResponse, RegionEndpoint>> Deploy(
            AWSCredentials credentials,
            List<RegionEndpoint> regions,
            Dictionary<string, string> environmentVariables,
            int? scheduledFrequencyInMinutes,
            string functionName,
            string projectPath,
            LambdaEntrypointDefinition entrypoint,
            string roleArn,
            Runtime runtime,
            int memorySizeMb)
        {
            var outputPath = LambdaSymphonyComposure.GetOutputPath(projectPath);

            var buildPath = BuildLambdaProject.Build(projectPath, outputPath, runtime);

            Console.WriteLine("Deploying: " + buildPath);
            List<Tuple<CreateFunctionResponse, RegionEndpoint>> responses;
            var lambdaSymphony = new LambdaSymphonyComposure();
            using (var zipArchiveBuildStream = new MemoryStream())
            using (var fs = File.OpenRead(buildPath))
            {
                var request = new CreateFunctionRequest
                {
                    FunctionName = functionName,
                    Runtime = runtime,
                    Handler = entrypoint.GetEntryPointHandler(),
                    Role = roleArn,
                    Code = new FunctionCode { ZipFile = zipArchiveBuildStream },
                    Timeout = 60 * 5,
                    MemorySize = memorySizeMb
                };

                fs.CopyTo(zipArchiveBuildStream);
                responses = lambdaSymphony
                    .CompileAndRebuildRemotely(
                        credentials,
                        regions,
                        functionName,
                        environmentVariables,
                        request);
            }

            if (scheduledFrequencyInMinutes.HasValue &&
                scheduledFrequencyInMinutes.Value > 0)
            {
                Parallel.ForEach(responses, createResponse =>
                {
                    lambdaSymphony.RebuildFunctionSchedule(
                        credentials,
                        createResponse.Item1.FunctionArn,
                        "Schedule" + functionName,
                        scheduledFrequencyInMinutes.Value,
                        createResponse.Item2);
                });
            }

            return responses;
        }
    }
}
