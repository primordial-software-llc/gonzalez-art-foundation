using System;
using System.IO;
using System.Linq;
using System.Threading;
using IndexBackend.Indexing;

namespace SlideshowIndexer
{
    class FileIdQueueIndexer
    {
        public void Index(IIndex indexer)
        {
            if (!File.Exists(indexer.IdFileQueuePath))
            {
                Console.WriteLine("Id Queue File is Required: " + indexer.IdFileQueuePath);
                return;
            }

            var idQueue = File.ReadAllLines(indexer.IdFileQueuePath).ToList();
                
            for (int i = idQueue.Count - 1; i >= 0; i--)
            {
                int id = int.Parse(idQueue[i]);
                Console.WriteLine("Classifying " + id);

                indexer.Index(id);
                idQueue.RemoveAt(i);

                File.WriteAllLines(indexer.IdFileQueuePath, idQueue);

                Console.WriteLine("Throttling.");
                Thread.Sleep(indexer.GetNextThrottleInMilliseconds);
            }

            Console.WriteLine("Indexing complete");
        }

    }
}
