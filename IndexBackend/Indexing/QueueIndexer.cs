using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using ArtApi.Model;
using IndexBackend.Sources.Rijksmuseum;
using Newtonsoft.Json;

namespace IndexBackend.Indexing
{
    public class QueueIndexer
    {
        private IAmazonSQS QueueClient { get; }
        private IndexingCore IndexingCore { get; }
        private HttpClient HttpClient { get; }
        private ILogging Logger { get; }
        private const string QUEUE_URL = "https://sqs.us-east-1.amazonaws.com/283733643774/gonzalez-art-foundation-crawler";
        private const int SQS_MAX_BATCH = 10;

        public QueueIndexer(IAmazonSQS queueClient, HttpClient httpClient, IndexingCore indexingCore, ILogging logger)
        {
            QueueClient = queueClient;
            IndexingCore = indexingCore;
            HttpClient = httpClient;
            Logger = logger;
        }

        public string ProcessAllInQueue()
        {
            ReceiveMessageResponse batch;
            do
            {
                batch = QueueClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    MaxNumberOfMessages = SQS_MAX_BATCH,
                    QueueUrl = QUEUE_URL
                }).Result;
                if (!batch.Messages.Any())
                {
                    break;
                }
                var tasks = new List<Task>();
                foreach (var message in batch.Messages)
                {
                    tasks.Add(IndexAndMarkComplete(message));
                }
                Task.WaitAll(tasks.ToArray());
            } while (batch.Messages.Any());
            return $"No additional SQS messages found in {QUEUE_URL}";
        }


        private async Task IndexAndMarkComplete(Message message)
        {
            try
            {
                var model = JsonConvert.DeserializeObject<ClassificationModel>(message.Body);
                var indexer = new IndexerFactory().GetIndexer(model.Source, HttpClient);
                if (indexer == null)
                {
                    Logger.Log($"Failed to process message due to unknown source {model.Source} for message: {message.Body}");
                    return;
                }

                await IndexingCore.Index(indexer, model);
            }
            catch (Exception e) when (e.InnerException is ProtectedClassificationException)
            {
                Logger.Log($"Skipping index request due to attempting to re-index protected classification: {e.Message} for message: {message.Body}. Error: " + e);
                await QueueClient.DeleteMessageAsync(QUEUE_URL, message.ReceiptHandle);
                return;
            }
            catch (Exception e) when (e.InnerException is StitchedImageException)
            {
                Logger.Log($"Failed to stitch image tiles together. The image can be reprocessed by the queue: {e.Message} for message: {message.Body}. Error: " + e);
                return;
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to process message due to unknown error: {e.Message} for message: {message.Body}. Error: " + e);
                return;
            }

            await QueueClient.DeleteMessageAsync(QUEUE_URL, message.ReceiptHandle);
        }
    }
}
