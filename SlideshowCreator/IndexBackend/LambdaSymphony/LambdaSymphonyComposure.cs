using System;
using System.Collections.Generic;
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
    class LambdaSymphonyComposure
    {
        public static string GetOutputPath(string projectPath)
        {
            var path = projectPath.Split('\\');
            path[path.Length - 1] = "bin";

            return string.Join(@"\", path);
        }

        public List<Tuple<CreateFunctionResponse, RegionEndpoint>> CompileAndRebuildRemotely(
            AWSCredentials credentials,
            List<RegionEndpoint> regions,
            string functionName,
            Dictionary<string, string> environmentVariables,
            CreateFunctionRequest request)
        {
            List<Tuple<CreateFunctionResponse, RegionEndpoint>> results = new List<Tuple<CreateFunctionResponse, RegionEndpoint>>();

            Parallel.ForEach(regions, region =>
            {
                CreateFunctionResponse createFunctionResponse = RebuildFunction(
                    credentials,
                    region,
                    functionName,
                    environmentVariables,
                    request);
                Console.WriteLine($"Deployed to {createFunctionResponse.FunctionName} in {region.DisplayName}");
                results.Add(new Tuple<CreateFunctionResponse, RegionEndpoint>(createFunctionResponse, region));
            });

            return results;
        }

        public PutRuleResponse RebuildFunctionSchedule(
            AWSCredentials credentials,
            string functionArn,
            string scheduleName,
            int frequencyInMinutes,
            RegionEndpoint region)
        {
            AmazonCloudWatchEventsClient cloudwatchClient = new AmazonCloudWatchEventsClient(
                credentials,
                region);

            var rules = cloudwatchClient.ListRules(new ListRulesRequest { NamePrefix = scheduleName });
            if (rules.Rules.Any())
            {
                var targetsForRule = cloudwatchClient.ListTargetsByRule(new ListTargetsByRuleRequest { Rule = scheduleName }).Targets;
                if (targetsForRule.Any())
                {
                    cloudwatchClient.RemoveTargets(new RemoveTargetsRequest { Ids = targetsForRule.Select(x => x.Id).ToList(), Rule = scheduleName });
                }
                var deleteResult = cloudwatchClient.DeleteRuleAsync(new DeleteRuleRequest { Name = scheduleName }).Result;
            }

            var increment = frequencyInMinutes == 1 ? "minute" : "minutes";
            var putRequest = new PutRuleRequest
            {
                Name = scheduleName,
                ScheduleExpression = $"rate({frequencyInMinutes} {increment})",
                State = RuleState.ENABLED
            };
            PutRuleResponse scheduleResponse = cloudwatchClient.PutRuleAsync(putRequest).Result;

            var putTargetRequest = new PutTargetsRequest
            {
                Rule = putRequest.Name,
                Targets = new List<Target> { new Target { Id = Guid.NewGuid().ToString(), Arn = functionArn } }
            };
            var targetResponse = cloudwatchClient.PutTargetsAsync(putTargetRequest).Result;
            if (targetResponse.FailedEntries.Any())
            {
                throw new Exception(GetFailureReason(targetResponse.FailedEntries));
            }

            AddPermissionForCloudWatchTriggerInvocation(credentials, region, functionArn, scheduleResponse);

            return scheduleResponse;
        }

        /// <remarks>
        /// The UI will add this permission automatically.
        /// Without this permission, a CloudWatch trigger will not show in the UI.
        /// </remarks>
        private void AddPermissionForCloudWatchTriggerInvocation(
            AWSCredentials credentials,
            RegionEndpoint region,
            string functionArn,
            PutRuleResponse putRuleResponse)
        {
            AmazonLambdaClient client = new AmazonLambdaClient(credentials, region);
            client.AddPermission(new AddPermissionRequest
            {
                Action = "lambda:InvokeFunction",
                FunctionName = functionArn,
                Principal = "events.amazonaws.com",
                SourceArn = putRuleResponse.RuleArn,
                StatementId = "default"
            });
        }

        private string GetFailureReason(List<PutTargetsResultEntry> failures)
        {
            return string.Join(", ", failures.Select(x => $"Error Message: {x.ErrorMessage} Error Code: {x.ErrorCode} Target ID: {x.TargetId}"));
        }

        private CreateFunctionResponse RebuildFunction(
            AWSCredentials credentials,
            RegionEndpoint region,
            string functionName,
            Dictionary<string, string> environmentVariables,
            CreateFunctionRequest createFunctionRequest)
        {
            AmazonLambdaClient client = new AmazonLambdaClient(credentials, region);

            if (new LambdaFunctionExists().FunctionExists(client, functionName))
            {
                client.DeleteFunction(functionName);
            }

            if (environmentVariables != null && environmentVariables.Any())
            {
                createFunctionRequest.Environment = new Amazon.Lambda.Model.Environment { Variables = environmentVariables };
            }

            var response = client.CreateFunctionAsync(createFunctionRequest).Result;

            return response;
        }

    }
}
