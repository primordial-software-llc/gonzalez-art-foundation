using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatchEvents;
using Amazon.CloudWatchEvents.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;

namespace IndexBackend.LambdaSymphony
{
    public class LambdaDeploy
    {
        public void Delete(
            AWSCredentials credentials,
            List<RegionEndpoint> regions,
            string functionName,
            int numberOfDeploymentsPerRegion)
        {
            Parallel.ForEach(regions, region =>
            {
                for (int ct = 0; ct < numberOfDeploymentsPerRegion; ct++)
                {
                    AmazonLambdaClient client = new AmazonLambdaClient(credentials, region);
                    if (new LambdaFunctionExists().FunctionExists(client, GetFunctionName(functionName, ct)))
                    {
                        client.DeleteFunction(GetFunctionName(functionName, ct));
                    }
                }
                new LambdaSymphonyComposure().DeleteFunctionSchedule(credentials, region, GetScheduleName(functionName));
            });
        }

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
            int memorySizeMb,
            int numberOfDeploymentsPerRegion,
            TimeSpan timeout)
        {
            List <Tuple<CreateFunctionResponse, RegionEndpoint>> deployments = new List<Tuple<CreateFunctionResponse, RegionEndpoint>>();
            var outputPath = LambdaSymphonyComposure.GetOutputPath(projectPath);
            var buildPath = BuildLambdaProject.Build(projectPath, outputPath, runtime);
            Console.WriteLine("Deploying: " + buildPath);

            for (int ct = 0; ct < numberOfDeploymentsPerRegion; ct++)
            {
                var deployment = DeployCore(
                    credentials,
                    regions,
                    environmentVariables,
                    GetFunctionName(functionName, ct),
                    entrypoint,
                    roleArn,
                    runtime,
                    memorySizeMb,
                    buildPath,
                    timeout);
                deployments.AddRange(deployment);
            }

            if (scheduledFrequencyInMinutes.HasValue &&
                scheduledFrequencyInMinutes.Value > 0)
            {
                var lambdaSymphony = new LambdaSymphonyComposure();
                Parallel.ForEach(regions, region =>
                {
                    lambdaSymphony.DeleteFunctionSchedule(credentials, region, GetScheduleName(functionName));
                });

                Dictionary<string, string> regionRuleDictionary = new Dictionary<string, string>();
                foreach (var region in regions)
                {
                    var increment = scheduledFrequencyInMinutes == 1 ? "minute" : "minutes";
                    var putRequest = new PutRuleRequest
                    {
                        Name = GetScheduleName(functionName),
                        ScheduleExpression = $"rate({scheduledFrequencyInMinutes} {increment})",
                        State = RuleState.ENABLED
                    };
                    AmazonCloudWatchEventsClient cloudwatchClient = new AmazonCloudWatchEventsClient(
                        credentials,
                        region);
                    PutRuleResponse scheduleResponse = cloudwatchClient.PutRuleAsync(putRequest).Result;
                    regionRuleDictionary.Add(region.SystemName, scheduleResponse.RuleArn);
                }

                foreach (var deployment in deployments)
                {
                    var scheduleArn = regionRuleDictionary[deployment.Item2.SystemName];
                    var region = regions.First(x => x.SystemName == deployment.Item2.SystemName);
                    lambdaSymphony.AssignFunctionSchedule(
                        credentials,
                        deployment.Item1.FunctionArn,
                        GetScheduleName(functionName),
                        scheduleArn,
                        region);
                }
            }

            return deployments;
        }

        private List<Tuple<CreateFunctionResponse, RegionEndpoint>> DeployCore(
            AWSCredentials credentials,
            List<RegionEndpoint> regions,
            Dictionary<string, string> environmentVariables,
            string functionName,
            LambdaEntrypointDefinition entrypoint,
            string roleArn,
            Runtime runtime,
            int memorySizeMb,
            string buildPath,
            TimeSpan timeout)
        {
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
                    Timeout = Convert.ToInt32(timeout.TotalSeconds),
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

            return responses;
        }

        private string GetFunctionName(string functionName, int deploymentNumberInRegion)
        {
            return $"{functionName}-{deploymentNumberInRegion}";
        }

        private string GetScheduleName(string functionName)
        {
            return "Schedule" + functionName;
        }
    }
}
