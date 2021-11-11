using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using ArtApi.Model;
using Newtonsoft.Json;

namespace IndexBackend
{
    public class Harvester
    {
        public static int SqsMaxMessages => 10;

        public void HarvestIntoSqs(IAmazonSQS sqsClient, string source, int start, int end)
        {
            for (var ct = start; ct < end + 1; ct += 10)
            {
                var batch = new List<ClassificationModel>();
                for (var pageId = ct; pageId < ct + 10 && pageId < end + 1; pageId++)
                {
                    batch.Add(new ClassificationModel
                    {
                        Source = source,
                        PageId = pageId.ToString()
                    });
                }

                SendBatch(
                    sqsClient,
                    "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler",
                    batch
                        .Select(crawlerModel =>
                            new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), JsonConvert.SerializeObject(crawlerModel, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore})))
                        .ToList()
                );
            }
        }

        public static SendMessageBatchResponse SendBatch(IAmazonSQS sqsClient, List<ClassificationModel> batch)
        {
            return SendBatch(
                sqsClient,
                "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler",
                batch
                    .Select(crawlerModel =>
                        new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), JsonConvert.SerializeObject(crawlerModel, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })))
                    .ToList()
            );
        }

        public static SendMessageBatchResponse SendBatch(IAmazonSQS queueClient, string queueUrl, List<SendMessageBatchRequestEntry> messages)
        {
            var sqsResult = queueClient.SendMessageBatchAsync(queueUrl, messages).Result;
            if (sqsResult.Failed.Any())
            {
                Console.WriteLine($"Failed to insert into SQS: {GetFailureReason(sqsResult.Failed)}.");
                Console.WriteLine("Retrying in 10 seconds");
                Thread.Sleep(TimeSpan.FromSeconds(10));

                var retryMessages = new List<SendMessageBatchRequestEntry>();
                foreach (BatchResultErrorEntry failedMessage in sqsResult.Failed)
                {
                    retryMessages.Add(messages.Single(x => x.Id == failedMessage.Id));
                }

                var retrySqsResult = queueClient.SendMessageBatchAsync(queueUrl, retryMessages).Result;
                if (retrySqsResult.Failed.Any())
                {
                    throw new Exception($"Failed to insert into SQS: {GetFailureReason(retrySqsResult.Failed)}.");
                }
            }
            return sqsResult;
        }

        private static string GetFailureReason(List<BatchResultErrorEntry> sqsFailures)
        {
            return string.Join(", ", sqsFailures.Select(x => $"Message: {x.Message} Sender's Fault: {x.SenderFault} Code: {x.Code}"));
        }
    }
}
