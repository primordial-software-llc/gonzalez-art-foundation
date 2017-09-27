using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IndexBackend.Indexing;

namespace SlideshowIndexer
{
    class FileIdQueueIndexer
    {
        private readonly Object dataLock = new Object();
        private readonly int maxLevelOfParallelism = 5;

        public void Index(IIndex indexer)
        {
            if (!File.Exists(indexer.IdFileQueuePath))
            {
                Console.WriteLine("Id Queue File is Required: " + indexer.IdFileQueuePath);
                return;
            }

            List<int> idQueue = File.ReadAllLines(indexer.IdFileQueuePath)
                .Select(int.Parse)
                .ToList();
            
            while (idQueue.Any())
            {
                List<int> nextBatch = idQueue.Take(maxLevelOfParallelism).ToList();

                try
                {
                    IndexBatch(indexer, nextBatch, idQueue);
                }
                catch (AggregateException aggregateException)
                {
                    AggregateException flattenedException = aggregateException.Flatten();
                    if (flattenedException.InnerExceptions.Any(ex => ex is HttpRequestException))
                    {
                        var exMsg = "HTTP EXCEPTION ENCOUNTERED:" +
                                    aggregateException;
                        Console.WriteLine(exMsg);
                        // I'm getting 503 response after backing off rapidly from the CloudFlare auth.
                        // My guess is that it's not the intermittent 503's I've been getting when testing
                        // (or perhaps it is and this is the cause of those).
                        // I backoff for a few minutes to let the DDOS protection cool-down.
                        var backOff = TimeSpan.FromMinutes(3);
                        Console.WriteLine($"BACKING OFF: {backOff.TotalMilliseconds}ms");
                        Thread.Sleep(backOff);
                        Console.WriteLine("BACKOFF COMPLETE - REFRESHING CONNECTION");
                        indexer.RefreshConnection();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            Console.WriteLine("Indexing complete");
        }
        
        private void IndexBatch(IIndex indexer, List<int> batch, List<int> idQueue)
        {
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxLevelOfParallelism
            };
            Parallel.ForEach(batch, parallelOptions, id =>
            {
                Index(indexer, id, idQueue);
            });
        }

        private void Index(IIndex indexer, int id, List<int> idQueue)
        {
            Console.WriteLine("Classifying: " + id);
            indexer.Index(id);

            Console.WriteLine("Updating queue: " + id);
            lock (dataLock)
            {
                idQueue.Remove(id);
                File.WriteAllLines(indexer.IdFileQueuePath, idQueue.Select(x => x.ToString()));
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
