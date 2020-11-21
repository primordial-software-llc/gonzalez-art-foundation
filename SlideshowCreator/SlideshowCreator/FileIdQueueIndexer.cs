using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndexBackend.Indexing;
using Polly;

namespace SlideshowCreator
{
    public class FileIdQueueIndexer
    {
        private readonly Object DataLock = new Object();
        private readonly int batchSize = 2;
        private readonly int maxParallelism = 1;

        public void Index(IIndex indexer, string idFileQueuePath)
        {
            if (!File.Exists(idFileQueuePath))
            {
                Console.WriteLine("Id Queue File is Required: " + idFileQueuePath);
                return;
            }

            List<int> idQueue = File.ReadAllLines(idFileQueuePath)
                .Select(int.Parse)
                .ToList();
            
            while (idQueue.Any())
            {
                List<int> nextBatch = idQueue.Take(batchSize).ToList();
                IndexBatch(indexer, nextBatch, idQueue, idFileQueuePath);
            }
            
            Console.WriteLine("Indexing complete");
        }
        
        private void IndexBatch(IIndex indexer, List<int> batch, List<int> idQueue, string idFileQueuePath)
        {
            SemaphoreSlim maxThread = new SemaphoreSlim(maxParallelism, maxParallelism);
            var tasks = new ConcurrentDictionary<int, Task>();

            foreach (var id in batch)
            {
                maxThread.Wait();
                var added = tasks.TryAdd(
                    id,
                    Index(indexer, id, idQueue, idFileQueuePath)
                    .ContinueWith(task => maxThread.Release())
                    .ContinueWith(task => tasks.TryRemove(id, out Task removedTask)) // I don't care if the task can't be removed it's just removed to prevent a memory issue with a large batch.
                );
                if (!added)
                {
                    throw new Exception("Failed to add task to concurrent dictionary");
                }
            }

            Task.WaitAll(tasks.Select(x => x.Value).ToArray());
        }

        private async Task Index(IIndex indexer, int id, List<int> idQueue, string idFileQueuePath)
        {
            Console.WriteLine("Classifying: " + id);

            var retryPolicy = Policy
                .Handle<AggregateException>(ex => ex.InnerExceptions.First() is TaskCanceledException)
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(15));

            await retryPolicy.ExecuteAsync(async () => await indexer.Index(id));

            Console.WriteLine("Updating file id queue: " + id);
            lock (DataLock)
            {
                idQueue.Remove(id);
                File.WriteAllLines(idFileQueuePath, idQueue.Select(x => x.ToString()));
            }
            Console.WriteLine("Done indexing: " + id);

            var throttleMs = indexer.GetNextThrottleInMilliseconds;
            if (throttleMs > 0)
            {
                Console.WriteLine("Throttling: " + id + " for " + throttleMs + "ms");
                Thread.Sleep(throttleMs);
            }
        }

    }
}
