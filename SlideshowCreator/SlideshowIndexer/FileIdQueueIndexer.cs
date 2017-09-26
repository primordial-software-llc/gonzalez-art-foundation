using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                catch (System.Net.Http.HttpRequestException httpEx)
                {
                    Console.WriteLine("Http exception encountered - re-initializing the http client and retrying: " + httpEx);
                    indexer.RefreshConnection();
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
