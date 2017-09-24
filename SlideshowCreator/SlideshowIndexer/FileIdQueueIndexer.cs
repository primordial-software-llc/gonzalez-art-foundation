using System;
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

        public void Index(IIndex indexer)
        {
            if (!File.Exists(indexer.IdFileQueuePath))
            {
                Console.WriteLine("Id Queue File is Required: " + indexer.IdFileQueuePath);
                return;
            }

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 6
            };

            var idQueue = File.ReadAllLines(indexer.IdFileQueuePath)
                .Select(int.Parse)
                .ToList();
            var iterableIdQueue = idQueue.ToList();

            Parallel.ForEach(iterableIdQueue, parallelOptions, id =>
            {
                Console.WriteLine("Classifying: " + id);
                indexer.Index(id);

                Console.WriteLine("Updating queue: " + id);
                lock (dataLock) // Doesn't really matter with only 5 threads on an SSD. I want to use SQS, but I'm finding ithard to justify when I can't go past 5 threads and the process isn't distributed. SQS was looking nice with 10+ threads, but network bottleneck is hit quick.
                { 
                    idQueue.Remove(id);
                    File.WriteAllLines(indexer.IdFileQueuePath, idQueue.Select(x => x.ToString()));
                }
                Console.WriteLine("Done indxig: " + id);

                var throttleMs = indexer.GetNextThrottleInMilliseconds;
                if (throttleMs > 0)
                {
                    Console.WriteLine("Throttling: " + id + " for " + throttleMs + "ms");
                    Thread.Sleep(throttleMs);
                }
            });

            Console.WriteLine("Indexing complete");
        }

    }
}
